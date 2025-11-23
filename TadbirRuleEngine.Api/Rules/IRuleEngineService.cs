using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Rules;

public interface IRuleEngineService
{
    Task<IEnumerable<RuleDto>> GetRulesByPolicyIdAsync(int policyId);
    Task<RuleDto?> GetRuleByIdAsync(int id);
    Task<RuleDto> CreateRuleAsync(CreateRuleDto dto);
    Task<RuleDto?> UpdateRuleAsync(int id, UpdateRuleDto dto);
    Task<bool> DeleteRuleAsync(int id);
    Task<bool> EvaluateRuleConditionAsync(int ruleId, Dictionary<string, object?> context);
    Task<object?> ExecuteRuleActionAsync(int ruleId, Dictionary<string, object?> context);
    Task<object?> ExecuteApiCallDirectAsync(int apiId, Dictionary<string, object?> context);
}