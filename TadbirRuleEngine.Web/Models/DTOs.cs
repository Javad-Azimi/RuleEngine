namespace TadbirRuleEngine.Web.Models;

// Swagger Source DTOs
public class SwaggerSourceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SwaggerUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

public class CreateSwaggerSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string SwaggerUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSwaggerSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string SwaggerUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

// API Definition DTOs
public class ApiDefinitionDto
{
    public int Id { get; set; }
    public int SwaggerSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RequestSchema { get; set; }
    public string? ResponseSchema { get; set; }
    public string? Parameters { get; set; }
    public bool RequiresAuth { get; set; }
    public DateTime CreatedAt { get; set; }
    public SwaggerSourceDto? SwaggerSource { get; set; }
}

// Policy DTOs
public class PolicyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AuthenticationSettingId { get; set; }
    public string? AuthenticationSettingName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public List<RuleDto> Rules { get; set; } = new();
}

public class CreatePolicyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AuthenticationSettingId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePolicyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AuthenticationSettingId { get; set; }
    public bool IsActive { get; set; }
}

// Rule DTOs
public class RuleDto
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int? ApiDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? ActionJson { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public ApiDefinitionDto? ApiDefinition { get; set; }
}

public class CreateRuleDto
{
    public int PolicyId { get; set; }
    public int? ApiDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? ActionJson { get; set; }
    public int Order { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateRuleDto
{
    public int? ApiDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? ActionJson { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

// Execution Log DTOs
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

// Authentication Setting DTOs
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