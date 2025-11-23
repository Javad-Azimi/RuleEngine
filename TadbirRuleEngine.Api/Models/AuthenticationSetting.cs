using System.ComponentModel.DataAnnotations;

namespace TadbirRuleEngine.Api.Models;

public class AuthenticationSetting
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string TokenEndpoint { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Password { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? GrantType { get; set; } = "password";
    
    [MaxLength(500)]
    public string? ClientId { get; set; }
    
    [MaxLength(500)]
    public string? ClientSecret { get; set; }
    
    [MaxLength(500)]
    public string? Scope { get; set; }
    
    public string? AdditionalParameters { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedAt { get; set; }
}