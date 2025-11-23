namespace TadbirRuleEngine.Api.DTOs;

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