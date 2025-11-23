using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Models;
using TadbirRuleEngine.Api.Rules;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RuleBuilderController : ControllerBase
{
    private readonly IRuleEngineService _ruleEngineService;
    private readonly ILogger<RuleBuilderController> _logger;

    public RuleBuilderController(
        IRuleEngineService ruleEngineService,
        ILogger<RuleBuilderController> logger)
    {
        _ruleEngineService = ruleEngineService;
        _logger = logger;
    }

    /// <summary>
    /// Extract field paths from API response for building conditions and mappings
    /// </summary>
    [HttpPost("extract-fields")]
    public IActionResult ExtractFields([FromBody] object apiResponse)
    {
        try
        {
            var fields = new List<FieldInfo>();
            ExtractFieldsRecursive(apiResponse, "", fields);
            
            return Ok(new { fields });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting fields from API response");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Build a structured condition from user-friendly input
    /// </summary>
    [HttpPost("build-condition")]
    public IActionResult BuildCondition([FromBody] BuildConditionRequest request)
    {
        try
        {
            var conditions = request.Conditions.Select(c => new RuleCondition
            {
                FieldPath = c.FieldPath,
                Operator = c.Operator,
                Value = c.Value,
                LogicalOperator = c.LogicalOperator
            }).ToList();

            var conditionJson = Newtonsoft.Json.JsonConvert.SerializeObject(conditions);
            
            // Also provide template-based version for backward compatibility
            var templateCondition = BuildTemplateCondition(conditions);

            return Ok(new 
            { 
                structuredCondition = conditionJson,
                templateCondition = templateCondition,
                preview = PreviewCondition(conditions)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building condition");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Build output mapping from field selections
    /// </summary>
    [HttpPost("build-mapping")]
    public IActionResult BuildMapping([FromBody] BuildMappingRequest request)
    {
        try
        {
            var mapping = new Dictionary<string, string>();
            
            foreach (var field in request.Fields)
            {
                var sourcePath = $"{{{{apiResult.{field.SourcePath}}}}}";
                if (!string.IsNullOrEmpty(field.Transform))
                {
                    sourcePath = $"{{{{{field.Transform}(apiResult.{field.SourcePath})}}}}";
                }
                mapping[field.TargetName] = sourcePath;
            }

            return Ok(new 
            { 
                mapping = mapping,
                json = Newtonsoft.Json.JsonConvert.SerializeObject(mapping, Newtonsoft.Json.Formatting.Indented)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building mapping");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test a condition against sample data
    /// </summary>
    [HttpPost("test-condition")]
    public async Task<IActionResult> TestCondition([FromBody] TestConditionRequest request)
    {
        try
        {
            var context = new Dictionary<string, object?>
            {
                ["apiResult"] = request.ApiResult
            };

            // Try structured condition first
            bool result;
            if (request.Condition.TrimStart().StartsWith("{") || request.Condition.TrimStart().StartsWith("["))
            {
                var conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<RuleCondition>>(
                    request.Condition.TrimStart().StartsWith("[") ? request.Condition : $"[{request.Condition}]");
                
                result = EvaluateStructuredConditions(conditions!, context);
            }
            else
            {
                // Use template-based evaluation
                result = await EvaluateTemplateCondition(request.Condition, context);
            }

            return Ok(new 
            { 
                result = result,
                message = result ? "Condition passed" : "Condition failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing condition");
            return BadRequest(new { error = ex.Message });
        }
    }

    private void ExtractFieldsRecursive(object? obj, string path, List<FieldInfo> fields, int depth = 0)
    {
        if (obj == null || depth > 10) return;

        if (obj is JObject jobj)
        {
            foreach (var prop in jobj.Properties())
            {
                var currentPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                var value = prop.Value;

                if (value.Type == JTokenType.Object || value.Type == JTokenType.Array)
                {
                    ExtractFieldsRecursive(value, currentPath, fields, depth + 1);
                }
                else
                {
                    fields.Add(new FieldInfo
                    {
                        Path = currentPath,
                        Type = value.Type.ToString(),
                        SampleValue = value.ToString(),
                        IsArray = false
                    });
                }
            }
        }
        else if (obj is JArray jarr && jarr.Count > 0)
        {
            // Extract fields from first array element
            var firstElement = jarr[0];
            var arrayPath = $"{path}[0]";
            ExtractFieldsRecursive(firstElement, arrayPath, fields, depth + 1);
        }
    }

    private string BuildTemplateCondition(List<RuleCondition> conditions)
    {
        var parts = new List<string>();
        
        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            var part = $"{{{{apiResult.{condition.FieldPath}}}}} {condition.Operator} \"{condition.Value}\"";
            parts.Add(part);
            
            if (i < conditions.Count - 1 && !string.IsNullOrEmpty(condition.LogicalOperator))
            {
                parts.Add(condition.LogicalOperator);
            }
        }
        
        return string.Join(" ", parts);
    }

    private string PreviewCondition(List<RuleCondition> conditions)
    {
        var parts = new List<string>();
        
        foreach (var condition in conditions)
        {
            var operatorText = condition.Operator switch
            {
                "==" => "equals",
                "!=" => "not equals",
                "contains" => "contains",
                "startsWith" => "starts with",
                "endsWith" => "ends with",
                _ => condition.Operator
            };
            
            parts.Add($"{condition.FieldPath} {operatorText} '{condition.Value}'");
            
            if (!string.IsNullOrEmpty(condition.LogicalOperator))
            {
                parts.Add(condition.LogicalOperator.ToUpper());
            }
        }
        
        return string.Join(" ", parts);
    }

    private bool EvaluateStructuredConditions(List<RuleCondition> conditions, Dictionary<string, object?> context)
    {
        bool? result = null;
        string? pendingOperator = null;

        foreach (var condition in conditions)
        {
            var fieldValue = GetValueFromPath($"apiResult.{condition.FieldPath}", context);
            var conditionResult = EvaluateSingleCondition(fieldValue, condition.Operator, condition.Value);

            if (result == null)
            {
                result = conditionResult;
            }
            else if (pendingOperator == "AND")
            {
                result = result.Value && conditionResult;
            }
            else if (pendingOperator == "OR")
            {
                result = result.Value || conditionResult;
            }

            pendingOperator = condition.LogicalOperator;
        }

        return result ?? true;
    }

    private bool EvaluateSingleCondition(object? fieldValue, string operatorType, string expectedValue)
    {
        var fieldStr = fieldValue?.ToString() ?? "";
        
        return operatorType.ToLower() switch
        {
            "==" or "equals" => string.Equals(fieldStr, expectedValue, StringComparison.OrdinalIgnoreCase),
            "!=" or "notequals" => !string.Equals(fieldStr, expectedValue, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldStr.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
            "startswith" => fieldStr.StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase),
            "endswith" => fieldStr.EndsWith(expectedValue, StringComparison.OrdinalIgnoreCase),
            ">" => double.TryParse(fieldStr, out var fv) && double.TryParse(expectedValue, out var ev) && fv > ev,
            "<" => double.TryParse(fieldStr, out var fv2) && double.TryParse(expectedValue, out var ev2) && fv2 < ev2,
            ">=" => double.TryParse(fieldStr, out var fv3) && double.TryParse(expectedValue, out var ev3) && fv3 >= ev3,
            "<=" => double.TryParse(fieldStr, out var fv4) && double.TryParse(expectedValue, out var ev4) && fv4 <= ev4,
            _ => false
        };
    }

    private object? GetValueFromPath(string path, Dictionary<string, object?> context)
    {
        var parts = path.Split('.');
        object? current = null;

        if (parts.Length > 0 && context.ContainsKey(parts[0]))
        {
            current = context[parts[0]];
            parts = parts.Skip(1).ToArray();
        }

        foreach (var part in parts)
        {
            if (current == null) break;

            var arrayMatch = System.Text.RegularExpressions.Regex.Match(part, @"^(\w+)\[(\d+)\]$");
            if (arrayMatch.Success)
            {
                var propertyName = arrayMatch.Groups[1].Value;
                var index = int.Parse(arrayMatch.Groups[2].Value);

                current = GetPropertyValue(current, propertyName);
                if (current != null)
                {
                    current = GetArrayElement(current, index);
                }
            }
            else
            {
                current = GetPropertyValue(current, part);
            }
        }

        return current;
    }

    private object? GetPropertyValue(object? obj, string propertyName)
    {
        if (obj == null) return null;

        if (obj is JObject jobj)
        {
            return jobj[propertyName];
        }
        else if (obj is JToken jtoken)
        {
            return jtoken[propertyName];
        }

        return null;
    }

    private object? GetArrayElement(object? obj, int index)
    {
        if (obj == null) return null;

        if (obj is JArray jarray && index >= 0 && index < jarray.Count)
        {
            return jarray[index];
        }

        return null;
    }

    private async Task<bool> EvaluateTemplateCondition(string condition, Dictionary<string, object?> context)
    {
        // Simple template evaluation - in production, use the MappingService
        var processed = condition;
        
        // Replace {{apiResult.path}} with actual values
        var matches = System.Text.RegularExpressions.Regex.Matches(condition, @"\{\{([^}]+)\}\}");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var expression = match.Groups[1].Value.Trim();
            var value = GetValueFromPath(expression, context);
            processed = processed.Replace(match.Value, value?.ToString() ?? "");
        }

        // Evaluate the condition
        if (processed.Contains("=="))
        {
            var parts = processed.Split("==");
            if (parts.Length == 2)
            {
                var left = parts[0].Trim().Trim('"');
                var right = parts[1].Trim().Trim('"');
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        return await Task.FromResult(false);
    }
}

public class FieldInfo
{
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SampleValue { get; set; } = string.Empty;
    public bool IsArray { get; set; }
}

public class BuildConditionRequest
{
    public List<ConditionDto> Conditions { get; set; } = new();
}

public class ConditionDto
{
    public string FieldPath { get; set; } = string.Empty;
    public string Operator { get; set; } = "==";
    public string Value { get; set; } = string.Empty;
    public string? LogicalOperator { get; set; }
}

public class BuildMappingRequest
{
    public List<FieldMappingDto> Fields { get; set; } = new();
}

public class TestConditionRequest
{
    public string Condition { get; set; } = string.Empty;
    public object ApiResult { get; set; } = new();
}
