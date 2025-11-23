namespace TadbirRuleEngine.Api.DTOs;

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