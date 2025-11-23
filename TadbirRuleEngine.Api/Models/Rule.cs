using System.ComponentModel.DataAnnotations;

namespace TadbirRuleEngine.Api.Models;

public class Rule
{
    public int Id { get; set; }
    
    public int PolicyId { get; set; }
    
    public int? ApiDefinitionId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public string? Condition { get; set; }
    
    public string? ActionJson { get; set; }
    
    public int Order { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Policy Policy { get; set; } = null!;
    public virtual ApiDefinition? ApiDefinition { get; set; }
}