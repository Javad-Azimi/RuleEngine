using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulesEngine.Models;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Mapping;
using TadbirRuleEngine.Api.Models;
using TadbirRuleEngine.Api.Services;

namespace TadbirRuleEngine.Api.Rules;

public class RuleEngineService : IRuleEngineService
{
    private readonly TadbirDbContext _context;
    private readonly IMappingService _mappingService;
    private readonly IAuthTokenService _authTokenService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RuleEngineService> _logger;

    public RuleEngineService(
        TadbirDbContext context,
        IMappingService mappingService,
        IAuthTokenService authTokenService,
        HttpClient httpClient,
        ILogger<RuleEngineService> logger)
    {
        _context = context;
        _mappingService = mappingService;
        _authTokenService = authTokenService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<RuleDto>> GetRulesByPolicyIdAsync(int policyId)
    {
        var rules = await _context.Rules
            .Include(r => r.ApiDefinition)
            .Where(r => r.PolicyId == policyId)
            .OrderBy(r => r.Order)
            .ToListAsync();

        return rules.Select(MapToDto);
    }

    public async Task<RuleDto?> GetRuleByIdAsync(int id)
    {
        var rule = await _context.Rules
            .Include(r => r.ApiDefinition)
            .FirstOrDefaultAsync(r => r.Id == id);

        return rule != null ? MapToDto(rule) : null;
    }

    public async Task<RuleDto> CreateRuleAsync(CreateRuleDto dto)
    {
        var rule = new Models.Rule
        {
            PolicyId = dto.PolicyId,
            ApiDefinitionId = dto.ApiDefinitionId,
            Name = dto.Name,
            Description = dto.Description,
            Condition = dto.Condition,
            ActionJson = dto.ActionJson,
            Order = dto.Order,
            IsActive = dto.IsActive
        };

        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Reload with includes
        rule = await _context.Rules
            .Include(r => r.ApiDefinition)
            .FirstAsync(r => r.Id == rule.Id);

        return MapToDto(rule);
    }

    public async Task<RuleDto?> UpdateRuleAsync(int id, UpdateRuleDto dto)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule == null) return null;

        rule.ApiDefinitionId = dto.ApiDefinitionId;
        rule.Name = dto.Name;
        rule.Description = dto.Description;
        rule.Condition = dto.Condition;
        rule.ActionJson = dto.ActionJson;
        rule.Order = dto.Order;
        rule.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        // Reload with includes
        rule = await _context.Rules
            .Include(r => r.ApiDefinition)
            .FirstAsync(r => r.Id == rule.Id);

        return MapToDto(rule);
    }

    public async Task<bool> DeleteRuleAsync(int id)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule == null) return false;

        _context.Rules.Remove(rule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EvaluateRuleConditionAsync(int ruleId, Dictionary<string, object?> context)
    {
        try
        {
            var rule = await _context.Rules.FindAsync(ruleId);
            if (rule == null || string.IsNullOrEmpty(rule.Condition))
                return true;

            var condition = rule.Condition.Trim();
            _logger.LogInformation("Evaluating condition: {Condition}", condition);
            
            // Enhanced condition evaluation with context
            return await EvaluateConditionExpression(condition, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule condition for rule {RuleId}", ruleId);
            return false;
        }
    }

    private async Task<bool> EvaluateConditionExpression(string condition, Dictionary<string, object?> context)
    {
        try
        {
            // Log the API result structure for debugging
            if (context.ContainsKey("apiResult"))
            {
                var apiResultJson = JsonConvert.SerializeObject(context["apiResult"], Formatting.Indented);
                _logger.LogInformation("API Result structure: {ApiResult}", apiResultJson);
            }

            // Try to parse as structured condition first
            if (condition.TrimStart().StartsWith("{") || condition.TrimStart().StartsWith("["))
            {
                try
                {
                    var structuredConditions = JsonConvert.DeserializeObject<List<Models.RuleCondition>>(
                        condition.TrimStart().StartsWith("[") ? condition : $"[{condition}]");
                    
                    if (structuredConditions != null && structuredConditions.Any())
                    {
                        return EvaluateStructuredConditions(structuredConditions, context);
                    }
                }
                catch
                {
                    // Fall back to template-based evaluation
                }
            }

            // Replace template expressions in condition with actual values
            var processedCondition = await _mappingService.RenderTemplateAsync(condition, context);
            _logger.LogInformation("Original condition: {Condition}", condition);
            _logger.LogInformation("Processed condition: {ProcessedCondition}", processedCondition);
            
            // Basic condition evaluation
            if (processedCondition.Contains("=="))
            {
                var parts = processedCondition.Split("==", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim().Trim('"');
                    var right = parts[1].Trim().Trim('"');
                    var result = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
                    _logger.LogInformation("Condition evaluation: '{Left}' == '{Right}' = {Result}", left, right, result);
                    return result;
                }
            }
            else if (processedCondition.Contains("!="))
            {
                var parts = processedCondition.Split("!=", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim().Trim('"');
                    var right = parts[1].Trim().Trim('"');
                    var result = !string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
                    _logger.LogInformation("Condition evaluation: '{Left}' != '{Right}' = {Result}", left, right, result);
                    return result;
                }
            }
            else if (processedCondition.Contains(">="))
            {
                var parts = processedCondition.Split(">=", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    var result = left >= right;
                    _logger.LogInformation("Condition evaluation: {Left} >= {Right} = {Result}", left, right, result);
                    return result;
                }
            }
            else if (processedCondition.Contains("<="))
            {
                var parts = processedCondition.Split("<=", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    var result = left <= right;
                    _logger.LogInformation("Condition evaluation: {Left} <= {Right} = {Result}", left, right, result);
                    return result;
                }
            }
            else if (processedCondition.Contains(">"))
            {
                var parts = processedCondition.Split(">", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    var result = left > right;
                    _logger.LogInformation("Condition evaluation: {Left} > {Right} = {Result}", left, right, result);
                    return result;
                }
            }
            else if (processedCondition.Contains("<"))
            {
                var parts = processedCondition.Split("<", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && 
                    double.TryParse(parts[0].Trim(), out var left) && 
                    double.TryParse(parts[1].Trim(), out var right))
                {
                    var result = left < right;
                    _logger.LogInformation("Condition evaluation: {Left} < {Right} = {Result}", left, right, result);
                    return result;
                }
            }
            
            // If no operators found, check if it's a boolean value or truthy
            if (bool.TryParse(processedCondition, out var boolResult))
            {
                return boolResult;
            }
            
            // Default: non-empty string is truthy
            var defaultResult = !string.IsNullOrWhiteSpace(processedCondition) && processedCondition != "null";
            _logger.LogInformation("Default condition evaluation: '{Condition}' = {Result}", processedCondition, defaultResult);
            return defaultResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in condition expression evaluation: {Condition}", condition);
            return false;
        }
    }

    private bool EvaluateStructuredConditions(List<Models.RuleCondition> conditions, Dictionary<string, object?> context)
    {
        bool? result = null;
        string? pendingOperator = null;

        foreach (var condition in conditions)
        {
            var fieldValue = _mappingService.EvaluateExpression($"apiResult.{condition.FieldPath}", context);
            var conditionResult = EvaluateSingleCondition(fieldValue, condition.Operator, condition.Value);

            _logger.LogInformation("Structured condition: {FieldPath} {Operator} {Value} = {Result} (actual value: {FieldValue})",
                condition.FieldPath, condition.Operator, condition.Value, conditionResult, fieldValue);

            if (result == null)
            {
                result = conditionResult;
            }
            else if (pendingOperator == "AND")
            {
                result = result.Value && conditionResult;
            }
            else if (pendingOperator == "OR")
            {
                result = result.Value || conditionResult;
            }

            pendingOperator = condition.LogicalOperator;
        }

        return result ?? true;
    }

    private bool EvaluateSingleCondition(object? fieldValue, string operatorType, string expectedValue)
    {
        var fieldStr = fieldValue?.ToString() ?? "";
        
        return operatorType.ToLower() switch
        {
            "==" or "equals" => string.Equals(fieldStr, expectedValue, StringComparison.OrdinalIgnoreCase),
            "!=" or "notequals" => !string.Equals(fieldStr, expectedValue, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldStr.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
            "startswith" => fieldStr.StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase),
            "endswith" => fieldStr.EndsWith(expectedValue, StringComparison.OrdinalIgnoreCase),
            ">" => double.TryParse(fieldStr, out var fv) && double.TryParse(expectedValue, out var ev) && fv > ev,
            "<" => double.TryParse(fieldStr, out var fv2) && double.TryParse(expectedValue, out var ev2) && fv2 < ev2,
            ">=" => double.TryParse(fieldStr, out var fv3) && double.TryParse(expectedValue, out var ev3) && fv3 >= ev3,
            "<=" => double.TryParse(fieldStr, out var fv4) && double.TryParse(expectedValue, out var ev4) && fv4 <= ev4,
            _ => false
        };
    }

    public async Task<object?> ExecuteRuleActionAsync(int ruleId, Dictionary<string, object?> context)
    {
        try
        {
            var rule = await _context.Rules
                .Include(r => r.ApiDefinition)
                .ThenInclude(a => a!.SwaggerSource)
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (rule == null || string.IsNullOrEmpty(rule.ActionJson))
                return null;

            var actionObj = JsonConvert.DeserializeObject<JObject>(rule.ActionJson);
            if (actionObj == null) return null;

            var actionType = actionObj["type"]?.ToString();
            
            if (actionType == "callApi" && rule.ApiDefinition != null)
            {
                return await ExecuteApiCallAsync(rule, actionObj, context);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rule action for rule {RuleId}", ruleId);
            return null;
        }
    }

    public async Task<object?> ExecuteApiCallDirectAsync(int apiId, Dictionary<string, object?> context)
    {
        try
        {
            var apiDef = await _context.ApiDefinitions
                .Include(a => a.SwaggerSource)
                .FirstOrDefaultAsync(a => a.Id == apiId);

            if (apiDef == null)
            {
                _logger.LogError("API definition not found: {ApiId}", apiId);
                return null;
            }

            _logger.LogInformation("Executing direct API call for API {ApiId}: {Name}", apiId, apiDef.Name);

            var baseUrl = GetBaseUrlFromSwagger(apiDef.SwaggerSource.SwaggerUrl);
            
            // Apply input mapping if provided, otherwise use previousResult directly
            object? requestData = null;
            var inputMapping = context.GetValueOrDefault("inputMapping");
            
            // Check if inputMapping is not null and not empty
            bool hasInputMapping = false;
            if (inputMapping != null)
            {
                // Check if it's an empty object
                if (inputMapping is JObject jobj && jobj.Count > 0)
                {
                    hasInputMapping = true;
                }
                else if (inputMapping is IDictionary<string, object> dict && dict.Count > 0)
                {
                    hasInputMapping = true;
                }
            }
            
            if (hasInputMapping)
            {
                // Apply input mapping to previousResult
                requestData = await _mappingService.ApplyMappingAsync(
                    context.GetValueOrDefault("previousResult"), 
                    inputMapping);
                _logger.LogInformation("Applied input mapping for test, data: {Data}", 
                    JsonConvert.SerializeObject(requestData));
            }
            else
            {
                // Use previousResult directly
                requestData = context.GetValueOrDefault("previousResult");
                _logger.LogInformation("Using previousResult directly as request data");
            }

            // Build URL with query parameters for GET requests
            var fullUrl = $"{baseUrl}{apiDef.Path}";
            if (apiDef.Method == "GET" && requestData != null)
            {
                // Convert requestData to query parameters
                var queryParams = new List<string>();
                if (requestData is JObject jobj)
                {
                    foreach (var prop in jobj.Properties())
                    {
                        var value = prop.Value.ToString();
                        // Convert parameter name to camelCase for API compatibility
                        var paramName = ToCamelCase(prop.Name);
                        queryParams.Add($"{paramName}={Uri.EscapeDataString(value)}");
                    }
                }
                else if (requestData is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        // Convert parameter name to camelCase for API compatibility
                        var paramName = ToCamelCase(kvp.Key);
                        queryParams.Add($"{paramName}={Uri.EscapeDataString(kvp.Value?.ToString() ?? "")}");
                    }
                }
                
                if (queryParams.Any())
                {
                    fullUrl += "?" + string.Join("&", queryParams);
                    _logger.LogInformation("Added query parameters to GET request");
                }
            }

            _logger.LogInformation("Calling API: {Method} {Url}", apiDef.Method, fullUrl);

            // Prepare HTTP request
            var request = new HttpRequestMessage(new HttpMethod(apiDef.Method), fullUrl);

            // Add auth token from context if available
            if (context.ContainsKey("authToken") && context["authToken"] != null)
            {
                var token = context["authToken"]!.ToString();
                request.Headers.Add("Authorization", $"Bearer {token}");
                _logger.LogInformation("Added Bearer token to request");
            }

            // Add request body for POST/PUT/PATCH
            if (requestData != null && (apiDef.Method == "POST" || apiDef.Method == "PUT" || apiDef.Method == "PATCH"))
            {
                var json = JsonConvert.SerializeObject(requestData);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                _logger.LogInformation("Added request body for {Method} request: {Body}", apiDef.Method, json);
            }

            // Execute request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("API response: {StatusCode}", response.StatusCode);
            _logger.LogInformation("API response content (first 500 chars): {Content}", 
                responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

            if (response.IsSuccessStatusCode)
            {
                // Deserialize to JObject to preserve structure
                var result = JsonConvert.DeserializeObject<JObject>(responseContent);
                
                // Log the deserialized result structure
                var resultJson = result?.ToString(Formatting.Indented) ?? "null";
                _logger.LogInformation("Deserialized API result: {Result}", 
                    resultJson.Length > 1000 ? resultJson.Substring(0, 1000) + "..." : resultJson);
                
                _logger.LogInformation("Direct API call successful");
                return result;
            }
            else
            {
                var errorMessage = $"API call failed: {response.StatusCode} - {responseContent}";
                _logger.LogError(errorMessage);
                throw new HttpRequestException(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing direct API call for API {ApiId}", apiId);
            throw;
        }
    }

    private async Task<object?> ExecuteApiCallAsync(Models.Rule rule, JObject actionObj, Dictionary<string, object?> context)
    {
        try
        {
            var apiDef = rule.ApiDefinition!;
            var baseUrl = GetBaseUrlFromSwagger(apiDef.SwaggerSource.SwaggerUrl);

            // Apply input mapping for request data if exists
            object? requestData = null;
            var inputMapping = actionObj["inputMapping"] ?? actionObj["requestMapping"];
            
            // Check if inputMapping is not null and not empty
            bool hasInputMapping = inputMapping != null && inputMapping.HasValues;
            
            if (hasInputMapping)
            {
                requestData = await _mappingService.ApplyMappingAsync(context.GetValueOrDefault("previousResult"), inputMapping);
                _logger.LogInformation("Applied input mapping, request data: {Data}", JsonConvert.SerializeObject(requestData));
            }
            else
            {
                // Use previousResult directly if no input mapping
                requestData = context.GetValueOrDefault("previousResult");
                _logger.LogInformation("Using previousResult directly as request data");
            }

            // Build URL with query parameters for GET requests
            var fullUrl = $"{baseUrl}{apiDef.Path}";
            if (apiDef.Method == "GET" && requestData != null)
            {
                // Convert requestData to query parameters
                var queryParams = new List<string>();
                if (requestData is JObject jobj)
                {
                    foreach (var prop in jobj.Properties())
                    {
                        var value = prop.Value.ToString();
                        // Convert parameter name to camelCase for API compatibility
                        var paramName = ToCamelCase(prop.Name);
                        queryParams.Add($"{paramName}={Uri.EscapeDataString(value)}");
                    }
                }
                else if (requestData is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        // Convert parameter name to camelCase for API compatibility
                        var paramName = ToCamelCase(kvp.Key);
                        queryParams.Add($"{paramName}={Uri.EscapeDataString(kvp.Value?.ToString() ?? "")}");
                    }
                }
                
                if (queryParams.Any())
                {
                    fullUrl += "?" + string.Join("&", queryParams);
                    _logger.LogInformation("Added query parameters to GET request: {Params}", string.Join("&", queryParams));
                }
            }

            _logger.LogInformation("Executing API call: {Method} {Url}", apiDef.Method, fullUrl);

            // Prepare HTTP request
            var request = new HttpRequestMessage(new HttpMethod(apiDef.Method), fullUrl);

            // Add auth token from context if available
            if (context.ContainsKey("authToken") && context["authToken"] != null)
            {
                var token = context["authToken"]!.ToString();
                request.Headers.Add("Authorization", $"Bearer {token}");
                _logger.LogInformation("Added Bearer token to request");
            }
            else if (apiDef.RequiresAuth)
            {
                // Fallback to auth service
                var authSettingName = context.GetValueOrDefault("authSettingName")?.ToString();
                if (!string.IsNullOrEmpty(authSettingName))
                {
                    var headers = await _authTokenService.GetAuthHeadersAsync(authSettingName);
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                    _logger.LogInformation("Added auth headers from auth service");
                }
            }

            // Add request body for POST/PUT/PATCH (GET uses query parameters)
            if (requestData != null && (apiDef.Method == "POST" || apiDef.Method == "PUT" || apiDef.Method == "PATCH"))
            {
                var json = JsonConvert.SerializeObject(requestData);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                _logger.LogInformation("Added request body for {Method} request", apiDef.Method);
            }

            // Execute request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("API response: {StatusCode}", response.StatusCode);
            _logger.LogInformation("API response content (first 500 chars): {Content}", 
                responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

            if (response.IsSuccessStatusCode)
            {
                // Deserialize to JObject to preserve structure
                var result = JsonConvert.DeserializeObject<JObject>(responseContent);
                
                // Log the deserialized result structure
                var resultJson = result?.ToString(Formatting.Indented) ?? "null";
                _logger.LogInformation("Deserialized API result: {Result}", 
                    resultJson.Length > 1000 ? resultJson.Substring(0, 1000) + "..." : resultJson);
                
                _logger.LogInformation("API call successful, result will be passed as previousResult to next rule");
                return result;
            }
            else
            {
                var errorMessage = $"API call failed: {response.StatusCode} - {responseContent}";
                _logger.LogError(errorMessage);
                
                // Throw exception to propagate error to policy executor
                throw new HttpRequestException(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing API call for rule {RuleId}", rule.Id);
            throw; // Re-throw to let policy executor handle it
        }
    }

    private string GetBaseUrlFromSwagger(string swaggerUrl)
    {
        try
        {
            var uri = new Uri(swaggerUrl);
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }
        catch
        {
            return "https://localhost";
        }
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;
        
        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    private static RuleDto MapToDto(Models.Rule rule)
    {
        return new RuleDto
        {
            Id = rule.Id,
            PolicyId = rule.PolicyId,
            ApiDefinitionId = rule.ApiDefinitionId,
            Name = rule.Name,
            Description = rule.Description,
            Condition = rule.Condition,
            ActionJson = rule.ActionJson,
            Order = rule.Order,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt,
            ApiDefinition = rule.ApiDefinition != null ? new ApiDefinitionDto
            {
                Id = rule.ApiDefinition.Id,
                SwaggerSourceId = rule.ApiDefinition.SwaggerSourceId,
                Name = rule.ApiDefinition.Name,
                Path = rule.ApiDefinition.Path,
                Method = rule.ApiDefinition.Method,
                Description = rule.ApiDefinition.Description,
                RequestSchema = rule.ApiDefinition.RequestSchema,
                ResponseSchema = rule.ApiDefinition.ResponseSchema,
                Parameters = rule.ApiDefinition.Parameters,
                RequiresAuth = rule.ApiDefinition.RequiresAuth,
                CreatedAt = rule.ApiDefinition.CreatedAt
            } : null
        };
    }
}