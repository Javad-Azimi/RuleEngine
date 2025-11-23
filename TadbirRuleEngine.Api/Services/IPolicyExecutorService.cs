using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Services;

public interface IPolicyExecutorService
{
    Task<IEnumerable<PolicyDto>> GetAllPoliciesAsync();
    Task<PolicyDto?> GetPolicyByIdAsync(int id);
    Task<PolicyDto> CreatePolicyAsync(CreatePolicyDto dto);
    Task<PolicyDto?> UpdatePolicyAsync(int id, UpdatePolicyDto dto);
    Task<bool> DeletePolicyAsync(int id);
    Task<object?> ExecutePolicyAsync(int policyId, Dictionary<string, object?>? initialContext = null);
}