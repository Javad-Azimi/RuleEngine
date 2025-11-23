using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Mapping;
using TadbirRuleEngine.Api.Models;
using TadbirRuleEngine.Api.Rules;

namespace TadbirRuleEngine.Api.Services;

public class PolicyExecutorService : IPolicyExecutorService
{
    private readonly TadbirDbContext _context;
    private readonly IRuleEngineService _ruleEngineService;
    private readonly IExecutionLogService _executionLogService;
    private readonly IAuthTokenService _authTokenService;
    private readonly IMappingService _mappingService;
    private readonly ILogger<PolicyExecutorService> _logger;

    public PolicyExecutorService(
        TadbirDbContext context,
        IRuleEngineService ruleEngineService,
        IExecutionLogService executionLogService,
        IAuthTokenService authTokenService,
        IMappingService mappingService,
        ILogger<PolicyExecutorService> logger)
    {
        _context = context;
        _ruleEngineService = ruleEngineService;
        _executionLogService = executionLogService;
        _authTokenService = authTokenService;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<IEnumerable<PolicyDto>> GetAllPoliciesAsync()
    {
        var policies = await _context.Policies
            .Include(p => p.AuthenticationSetting)
            .Include(p => p.Rules)
            .ThenInclude(r => r.ApiDefinition)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return policies.Select(MapToDto);
    }

    public async Task<PolicyDto?> GetPolicyByIdAsync(int id)
    {
        var policy = await _context.Policies
            .Include(p => p.AuthenticationSetting)
            .Include(p => p.Rules)
            .ThenInclude(r => r.ApiDefinition)
            .FirstOrDefaultAsync(p => p.Id == id);

        return policy != null ? MapToDto(policy) : null;
    }

    public async Task<PolicyDto> CreatePolicyAsync(CreatePolicyDto dto)
    {
        var policy = new Policy
        {
            Name = dto.Name,
            Description = dto.Description,
            AuthenticationSettingId = dto.AuthenticationSettingId,
            IsActive = dto.IsActive
        };

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();

        return MapToDto(policy);
    }

    public async Task<PolicyDto?> UpdatePolicyAsync(int id, UpdatePolicyDto dto)
    {
        var policy = await _context.Policies.FindAsync(id);
        if (policy == null) return null;

        policy.Name = dto.Name;
        policy.Description = dto.Description;
        policy.AuthenticationSettingId = dto.AuthenticationSettingId;
        policy.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        // Reload with includes
        policy = await _context.Policies
            .Include(p => p.AuthenticationSetting)
            .Include(p => p.Rules)
            .ThenInclude(r => r.ApiDefinition)
            .FirstAsync(p => p.Id == policy.Id);

        return MapToDto(policy);
    }

    public async Task<bool> DeletePolicyAsync(int id)
    {
        var policy = await _context.Policies.FindAsync(id);
        if (policy == null) return false;

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object?> ExecutePolicyAsync(int policyId, Dictionary<string, object?>? initialContext = null)
    {
        var startTime = DateTime.UtcNow;
        var context = initialContext ?? new Dictionary<string, object?>();
        var ruleResults = new List<object?>();
        object? lastResult = null;

        try
        {
            var policy = await _context.Policies
                .Include(p => p.AuthenticationSetting)
                .Include(p => p.Rules.OrderBy(r => r.Order))
                .ThenInclude(r => r.ApiDefinition)
                .FirstOrDefaultAsync(p => p.Id == policyId);

            if (policy == null)
            {
                await _executionLogService.LogExecutionAsync(policyId, null, "Failed", 
                    null, null, "Policy not found", startTime, DateTime.UtcNow);
                return null;
            }

            if (!policy.IsActive)
            {
                await _executionLogService.LogExecutionAsync(policyId, null, "Skipped", 
                    null, null, "Policy is inactive", startTime, DateTime.UtcNow);
                return null;
            }

            _logger.LogInformation("Starting execution of policy {PolicyId}: {PolicyName}", 
                policyId, policy.Name);

            // Step 1: Acquire authentication token if configured
            if (policy.AuthenticationSettingId.HasValue)
            {
                _logger.LogInformation("Acquiring authentication token for policy {PolicyId}", policyId);
                
                var token = await _authTokenService.AcquireTokenAsync(policy.AuthenticationSettingId.Value);
                if (string.IsNullOrEmpty(token))
                {
                    var errorMsg = "Failed to acquire authentication token";
                    _logger.LogError(errorMsg);
                    await _executionLogService.LogExecutionAsync(policyId, null, "Failed", 
                        initialContext, null, errorMsg, startTime, DateTime.UtcNow);
                    return new { success = false, error = errorMsg };
                }

                // Store token in context for rules to use
                context["authToken"] = token;
                context["authSettingName"] = policy.AuthenticationSetting?.Name;
                _logger.LogInformation("Authentication token acquired successfully");
            }

            // Step 2: Execute rules sequentially
            var activeRules = policy.Rules.Where(r => r.IsActive).OrderBy(r => r.Order).ToList();
            
            _logger.LogInformation("Executing {Count} active rules", activeRules.Count);

            foreach (var rule in activeRules)
            {
                var ruleStartTime = DateTime.UtcNow;
                
                try
                {
                    _logger.LogInformation("Executing rule {RuleId}: {RuleName} (Order: {Order})", 
                        rule.Id, rule.Name, rule.Order);

                    // Step 2.1: Update context with previous result
                    context["previousResult"] = lastResult;
                    
                    _logger.LogInformation("Rule {RuleId} starting with previousResult: {PreviousResult}", 
                        rule.Id, lastResult != null ? Newtonsoft.Json.JsonConvert.SerializeObject(lastResult) : "null");
                    
                    // Step 2.2: Evaluate condition BEFORE API call if it references previousResult
                    bool shouldExecute = true;
                    if (!string.IsNullOrEmpty(rule.Condition))
                    {
                        // Check if condition references previousResult (pre-execution condition)
                        if (rule.Condition.Contains("previousResult"))
                        {
                            _logger.LogInformation("Evaluating pre-execution condition for rule {RuleId}: {Condition}", 
                                rule.Id, rule.Condition);
                            
                            shouldExecute = await _ruleEngineService.EvaluateRuleConditionAsync(rule.Id, context);
                            
                            if (!shouldExecute)
                            {
                                _logger.LogInformation("Rule {RuleId} pre-execution condition not met, skipping rule entirely", rule.Id);
                                
                                ruleResults.Add(new { 
                                    ruleId = rule.Id, 
                                    ruleName = rule.Name, 
                                    order = rule.Order,
                                    success = true,
                                    skipped = true,
                                    reason = "Pre-execution condition not met"
                                });
                                
                                await _executionLogService.LogExecutionAsync(policyId, rule.Id, "Skipped", 
                                    context, null, "Pre-execution condition not met", ruleStartTime, DateTime.UtcNow);
                                
                                continue; // Skip this rule entirely
                            }
                            
                            _logger.LogInformation("Rule {RuleId} pre-execution condition met, proceeding with API call", rule.Id);
                        }
                    }
                    
                    // Step 2.3: Execute API call
                    var apiResult = await _ruleEngineService.ExecuteRuleActionAsync(rule.Id, context);
                    
                    _logger.LogInformation("Rule {RuleId} API call completed", rule.Id);

                    // Step 2.4: Apply output mapping FIRST (before condition check)
                    var finalResult = apiResult;
                    
                    if (!string.IsNullOrEmpty(rule.ActionJson))
                    {
                        try
                        {
                            var actionObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(rule.ActionJson);
                            var outputMapping = actionObj?["mapping"] ?? actionObj?["outputMapping"];
                            
                            if (outputMapping != null && outputMapping.HasValues)
                            {
                                _logger.LogInformation("Applying output mapping for rule {RuleId}", rule.Id);
                                
                                var mappingContext = new Dictionary<string, object?>(context)
                                {
                                    ["apiResult"] = apiResult,
                                    ["currentResult"] = apiResult,
                                    ["previousResult"] = lastResult
                                };
                                
                                finalResult = await _mappingService.ApplyMappingAsync(apiResult, outputMapping, mappingContext);
                                _logger.LogInformation("Output mapping applied successfully for rule {RuleId}: {Result}", 
                                    rule.Id, Newtonsoft.Json.JsonConvert.SerializeObject(finalResult));
                            }
                        }
                        catch (Exception mappingEx)
                        {
                            _logger.LogWarning(mappingEx, "Error applying output mapping for rule {RuleId}, using original API result", rule.Id);
                            finalResult = apiResult;
                        }
                    }

                    // Step 2.5: Evaluate condition on API result (if condition exists and references apiResult)
                    if (!string.IsNullOrEmpty(rule.Condition) && !rule.Condition.Contains("previousResult"))
                    {
                        _logger.LogInformation("Evaluating condition for rule {RuleId}: {Condition}", 
                            rule.Id, rule.Condition);
                        
                        // Create context with API result for condition evaluation
                        var conditionContext = new Dictionary<string, object?>(context)
                        {
                            ["apiResult"] = apiResult,
                            ["currentResult"] = apiResult
                        };
                        
                        var conditionMet = await _ruleEngineService.EvaluateRuleConditionAsync(rule.Id, conditionContext);
                        
                        if (!conditionMet)
                        {
                            _logger.LogInformation("Rule {RuleId} condition not met, but output mapping was applied. Result will not be passed to next rule.", rule.Id);
                            
                            ruleResults.Add(new { 
                                ruleId = rule.Id, 
                                ruleName = rule.Name, 
                                order = rule.Order,
                                success = true,
                                skipped = true,
                                reason = "Condition not met on API result",
                                apiResult = apiResult,
                                mappedResult = finalResult
                            });
                            
                            await _executionLogService.LogExecutionAsync(policyId, rule.Id, "Skipped", 
                                conditionContext, apiResult, "Condition not met", ruleStartTime, DateTime.UtcNow);
                            
                            continue; // Don't pass result to next rule
                        }
                        
                        _logger.LogInformation("Rule {RuleId} condition met, proceeding with result", rule.Id);
                    }

                    // Step 2.6: Pass result to next rule
                    
                    // Step 2.7: Pass result to next rule
                    if (finalResult != null)
                    {
                        lastResult = finalResult;
                        ruleResults.Add(new { 
                            ruleId = rule.Id, 
                            ruleName = rule.Name, 
                            order = rule.Order,
                            success = true,
                            result = finalResult
                        });
                        context["lastResult"] = finalResult;
                        context["previousResult"] = finalResult; // Update previousResult immediately for next rule
                        
                        _logger.LogInformation("Rule {RuleId} executed successfully, result: {Result}", 
                            rule.Id, Newtonsoft.Json.JsonConvert.SerializeObject(finalResult));
                        _logger.LogInformation("Result passed to next rule as previousResult");
                    }
                    else
                    {
                        _logger.LogWarning("Rule {RuleId} returned null result", rule.Id);
                        ruleResults.Add(new { 
                            ruleId = rule.Id, 
                            ruleName = rule.Name, 
                            order = rule.Order,
                            success = false,
                            error = "No result returned"
                        });
                    }

                    await _executionLogService.LogExecutionAsync(policyId, rule.Id, "Success", 
                        context, finalResult, null, ruleStartTime, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing rule {RuleId}: {RuleName}", rule.Id, rule.Name);
                    
                    await _executionLogService.LogExecutionAsync(policyId, rule.Id, "Failed", 
                        context, null, ex.Message, ruleStartTime, DateTime.UtcNow);
                    
                    // Add error to results
                    var errorResult = new { 
                        ruleId = rule.Id, 
                        ruleName = rule.Name, 
                        order = rule.Order,
                        error = ex.Message,
                        success = false
                    };
                    ruleResults.Add(errorResult);
                    
                    // Return failure immediately - stop execution on error
                    var failureResult = new
                    {
                        policyId = policyId,
                        policyName = policy.Name,
                        executedAt = DateTime.UtcNow,
                        success = false,
                        error = $"Rule '{rule.Name}' failed: {ex.Message}",
                        failedAtRule = rule.Order,
                        rulesExecuted = ruleResults.Count,
                        allResults = ruleResults,
                        context = context
                    };

                    await _executionLogService.LogExecutionAsync(policyId, null, "Failed", 
                        initialContext, failureResult, ex.Message, startTime, DateTime.UtcNow);

                    return failureResult;
                }
            }

            // Step 3: Update policy last executed time
            policy.LastExecutedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Step 4: Check if all rules succeeded
            var hasFailures = ruleResults.Any(r => 
            {
                if (r is IDictionary<string, object> dict)
                    return dict.ContainsKey("success") && dict["success"]?.ToString() == "False";
                var successProp = r?.GetType().GetProperty("success");
                return successProp != null && !(bool)(successProp.GetValue(r) ?? true);
            });

            var finalStatus = hasFailures ? "Completed with errors" : "Success";

            // Step 5: Prepare final result
            var policyResult = new
            {
                policyId = policyId,
                policyName = policy.Name,
                executedAt = DateTime.UtcNow,
                success = !hasFailures,
                status = finalStatus,
                rulesExecuted = activeRules.Count,
                rulesSucceeded = ruleResults.Count(r => 
                {
                    if (r is IDictionary<string, object> dict)
                        return dict.ContainsKey("success") && dict["success"]?.ToString() == "True";
                    var successProp = r?.GetType().GetProperty("success");
                    return successProp != null && (bool)(successProp.GetValue(r) ?? false);
                }),
                lastResult = lastResult,
                allResults = ruleResults,
                context = context
            };

            await _executionLogService.LogExecutionAsync(policyId, null, finalStatus, 
                initialContext, policyResult, null, startTime, DateTime.UtcNow);

            _logger.LogInformation("Policy {PolicyId} execution completed. Status: {Status}, Executed {Count} rules", 
                policyId, finalStatus, activeRules.Count);
            
            return policyResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing policy {PolicyId}", policyId);
            
            var errorResult = new
            {
                policyId = policyId,
                success = false,
                error = ex.Message,
                executedAt = DateTime.UtcNow
            };

            await _executionLogService.LogExecutionAsync(policyId, null, "Failed", 
                initialContext, null, ex.Message, startTime, DateTime.UtcNow);
            
            return errorResult;
        }
    }

    private static PolicyDto MapToDto(Policy policy)
    {
        return new PolicyDto
        {
            Id = policy.Id,
            Name = policy.Name,
            Description = policy.Description,
            AuthenticationSettingId = policy.AuthenticationSettingId,
            AuthenticationSettingName = policy.AuthenticationSetting?.Name,
            IsActive = policy.IsActive,
            CreatedAt = policy.CreatedAt,
            LastExecutedAt = policy.LastExecutedAt,
            Rules = policy.Rules?.Select(r => new RuleDto
            {
                Id = r.Id,
                PolicyId = r.PolicyId,
                ApiDefinitionId = r.ApiDefinitionId,
                Name = r.Name,
                Description = r.Description,
                Condition = r.Condition,
                ActionJson = r.ActionJson,
                Order = r.Order,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                ApiDefinition = r.ApiDefinition != null ? new ApiDefinitionDto
                {
                    Id = r.ApiDefinition.Id,
                    SwaggerSourceId = r.ApiDefinition.SwaggerSourceId,
                    Name = r.ApiDefinition.Name,
                    Path = r.ApiDefinition.Path,
                    Method = r.ApiDefinition.Method,
                    Description = r.ApiDefinition.Description,
                    RequestSchema = r.ApiDefinition.RequestSchema,
                    ResponseSchema = r.ApiDefinition.ResponseSchema,
                    Parameters = r.ApiDefinition.Parameters,
                    RequiresAuth = r.ApiDefinition.RequiresAuth,
                    CreatedAt = r.ApiDefinition.CreatedAt
                } : null
            }).ToList() ?? new List<RuleDto>()
        };
    }
}