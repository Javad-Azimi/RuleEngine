namespace TadbirRuleEngine.Api.DTOs;

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