using System.ComponentModel.DataAnnotations;

namespace TadbirRuleEngine.Api.Models;

public class Policy
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public int? AuthenticationSettingId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastExecutedAt { get; set; }
    
    // Navigation properties
    public virtual AuthenticationSetting? AuthenticationSetting { get; set; }
    public virtual ICollection<Rule> Rules { get; set; } = new List<Rule>();
    public virtual ICollection<PolicySchedule> Schedules { get; set; } = new List<PolicySchedule>();
    public virtual ICollection<ExecutionLog> ExecutionLogs { get; set; } = new List<ExecutionLog>();
}