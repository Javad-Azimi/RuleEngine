namespace TadbirRuleEngine.Api.Models;

/// <summary>
/// Represents a structured condition for rule evaluation
/// </summary>
public class RuleCondition
{
    /// <summary>
    /// The field path to evaluate (e.g., "Data.ProfileList[0].PositionTitle")
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// The operator to use for comparison (==, !=, >, <, >=, <=, contains, startsWith, endsWith)
    /// </summary>
    public string Operator { get; set; } = "==";

    /// <summary>
    /// The value to compare against
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Logical operator to combine with next condition (AND, OR)
    /// </summary>
    public string? LogicalOperator { get; set; }
}
