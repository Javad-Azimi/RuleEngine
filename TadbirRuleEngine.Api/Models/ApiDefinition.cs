using System.ComponentModel.DataAnnotations;

namespace TadbirRuleEngine.Api.Models;

public class ApiDefinition
{
    public int Id { get; set; }
    
    public int SwaggerSourceId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string Method { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public string? RequestSchema { get; set; }
    
    public string? ResponseSchema { get; set; }
    
    public string? Parameters { get; set; }
    
    public bool RequiresAuth { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual SwaggerSource SwaggerSource { get; set; } = null!;
    public virtual ICollection<Rule> Rules { get; set; } = new List<Rule>();
}