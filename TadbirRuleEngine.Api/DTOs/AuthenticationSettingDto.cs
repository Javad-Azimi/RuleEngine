namespace TadbirRuleEngine.Api.DTOs;

public class AuthenticationSettingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? GrantType { get; set; }
    public string? ClientId { get; set; }
    public string? Scope { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class CreateAuthenticationSettingDto
{
    public string Name { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? GrantType { get; set; } = "password";
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; }
    public string? AdditionalParameters { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateAuthenticationSettingDto
{
    public string Name { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? GrantType { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; }
    public string? AdditionalParameters { get; set; }
    public bool IsActive { get; set; }
}