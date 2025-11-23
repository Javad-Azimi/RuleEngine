using System.ComponentModel.DataAnnotations;

namespace TadbirRuleEngine.Api.Models;

public class SwaggerSource
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string SwaggerUrl { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<ApiDefinition> ApiDefinitions { get; set; } = new List<ApiDefinition>();
}