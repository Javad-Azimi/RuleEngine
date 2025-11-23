using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Schedulers;

public interface ISchedulerService
{
    Task<IEnumerable<PolicyScheduleDto>> GetSchedulesAsync();
    Task<PolicyScheduleDto?> GetScheduleByIdAsync(int id);
    Task<PolicyScheduleDto> CreateScheduleAsync(CreatePolicyScheduleDto dto);
    Task<PolicyScheduleDto?> UpdateScheduleAsync(int id, UpdatePolicyScheduleDto dto);
    Task<bool> DeleteScheduleAsync(int id);
    Task StartSchedulerAsync();
    Task StopSchedulerAsync();
}

public class PolicyScheduleDto
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PolicyName { get; set; }
}

public class CreatePolicyScheduleDto
{
    public int PolicyId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdatePolicyScheduleDto
{
    public string CronExpression { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}