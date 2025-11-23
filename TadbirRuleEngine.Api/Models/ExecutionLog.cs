using System.ComponentModel.DataAnnotations;

namespace TadbirRuleEngine.Api.Models;

public class ExecutionLog
{
    public int Id { get; set; }
    
    public int PolicyId { get; set; }
    
    public int? RuleId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Success, Failed, Running
    
    public string? Input { get; set; }
    
    public string? Output { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public int DurationMs { get; set; } = 0;
    
    // Navigation properties
    public virtual Policy Policy { get; set; } = null!;
    public virtual Rule? Rule { get; set; }
}