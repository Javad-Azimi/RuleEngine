using System.ComponentModel.DataAnnotations;

namespace TadbirRuleEngine.Api.Models;

public class PolicySchedule
{
    public int Id { get; set; }
    
    public int PolicyId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string CronExpression { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? NextRunAt { get; set; }
    
    public DateTime? LastRunAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Policy Policy { get; set; } = null!;
}