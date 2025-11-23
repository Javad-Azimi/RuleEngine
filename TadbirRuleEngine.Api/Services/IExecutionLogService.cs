using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Services;

public interface IExecutionLogService
{
    Task<IEnumerable<ExecutionLogDto>> GetLogsAsync(int? policyId = null, int skip = 0, int take = 50);
    Task LogExecutionAsync(int policyId, int? ruleId, string status, 
        object? input, object? output, string? errorMessage, 
        DateTime startedAt, DateTime completedAt);
}

public class ExecutionLogDto
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int? RuleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Input { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int DurationMs { get; set; }
    public string? PolicyName { get; set; }
    public string? RuleName { get; set; }
}