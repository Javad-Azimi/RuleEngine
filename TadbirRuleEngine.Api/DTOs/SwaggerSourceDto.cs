namespace TadbirRuleEngine.Api.DTOs;

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