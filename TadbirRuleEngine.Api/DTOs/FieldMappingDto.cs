namespace TadbirRuleEngine.Api.DTOs;

/// <summary>
/// Represents a field mapping for input or output
/// </summary>
public class FieldMappingDto
{
    /// <summary>
    /// The source field path (e.g., "Data.ProfileList[0].UserId")
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// The target field name (e.g., "UserId")
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Optional transformation function (toString, toNumber, etc.)
    /// </summary>
    public string? Transform { get; set; }
}
