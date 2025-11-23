using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace TadbirRuleEngine.Api.Mapping;

public class MappingService : IMappingService
{
    private readonly ILogger<MappingService> _logger;

    public MappingService(ILogger<MappingService> logger)
    {
        _logger = logger;
    }

    public async Task<object?> ApplyMappingAsync(object? source, object? mappingDefinition, Dictionary<string, object?>? context = null)
    {
        try
        {
            if (source == null || mappingDefinition == null)
                return source;

            // Use provided context or create a new one
            var mappingContext = context ?? new Dictionary<string, object?>();
            
            // Ensure apiResult is set (for backward compatibility)
            if (!mappingContext.ContainsKey("apiResult"))
            {
                mappingContext["apiResult"] = source;
            }
            
            // Ensure previousResult is set
            if (!mappingContext.ContainsKey("previousResult"))
            {
                mappingContext["previousResult"] = source;
            }

            var mappingJson = mappingDefinition is string str ? str : JsonConvert.SerializeObject(mappingDefinition);
            var mappingObj = JsonConvert.DeserializeObject<JToken>(mappingJson);

            var result = await ProcessMappingTokenAsync(mappingObj, mappingContext);
            
            _logger.LogInformation("Mapping applied successfully. Result: {Result}", JsonConvert.SerializeObject(result));
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying mapping");
            return source;
        }
    }

    private async Task<object?> ProcessMappingTokenAsync(JToken? token, Dictionary<string, object?> context)
    {
        if (token == null) return null;

        switch (token.Type)
        {
            case JTokenType.Object:
                var result = new Dictionary<string, object?>();
                foreach (var property in token.Children<JProperty>())
                {
                    var value = await ProcessMappingTokenAsync(property.Value, context);
                    result[property.Name] = value;
                }
                return result;

            case JTokenType.Array:
                var array = new List<object?>();
                foreach (var item in token.Children())
                {
                    var value = await ProcessMappingTokenAsync(item, context);
                    array.Add(value);
                }
                return array;

            case JTokenType.String:
                var stringValue = token.Value<string>() ?? "";
                return await RenderTemplateAsync(stringValue, context);

            default:
                return token.ToObject<object>();
        }
    }

    public async Task<string> RenderTemplateAsync(string template, Dictionary<string, object?> context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        // Match {{expression}} patterns
        var pattern = @"\{\{([^}]+)\}\}";
        var matches = Regex.Matches(template, pattern);

        var result = template;
        foreach (Match match in matches)
        {
            var expression = match.Groups[1].Value.Trim();
            var value = EvaluateExpression(expression, context);
            var stringValue = value?.ToString() ?? "";
            result = result.Replace(match.Value, stringValue);
        }

        return await Task.FromResult(result);
    }

    public object? EvaluateExpression(string expression, Dictionary<string, object?> context)
    {
        try
        {
            // Handle function calls
            if (expression.Contains("(") && expression.Contains(")"))
            {
                return EvaluateFunction(expression, context);
            }

            // Handle property paths like "previousResult.customer.name"
            return GetValueFromPath(expression, context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating expression: {Expression}", expression);
            return null;
        }
    }

    private object? EvaluateFunction(string expression, Dictionary<string, object?> context)
    {
        var functionMatch = Regex.Match(expression, @"(\w+)\((.*)\)");
        if (!functionMatch.Success) return null;

        var functionName = functionMatch.Groups[1].Value;
        var argsString = functionMatch.Groups[2].Value;
        var args = ParseFunctionArgs(argsString, context);

        return functionName.ToLower() switch
        {
            "tostring" => args.FirstOrDefault()?.ToString() ?? "",
            "tonumber" => ConvertToNumber(args.FirstOrDefault()),
            "concat" => string.Join("", args.Select(a => a?.ToString() ?? "")),
            "datenow" => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            "formatdate" => FormatDate(args),
            "if" => EvaluateIf(args),
            _ => null
        };
    }

    private List<object?> ParseFunctionArgs(string argsString, Dictionary<string, object?> context)
    {
        if (string.IsNullOrWhiteSpace(argsString))
            return new List<object?>();

        var args = new List<object?>();
        var parts = argsString.Split(',');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
            {
                // String literal
                args.Add(trimmed.Substring(1, trimmed.Length - 2));
            }
            else if (double.TryParse(trimmed, out var number))
            {
                // Number literal
                args.Add(number);
            }
            else
            {
                // Expression or path
                args.Add(GetValueFromPath(trimmed, context));
            }
        }

        return args;
    }

    private object? GetValueFromPath(string path, Dictionary<string, object?> context)
    {
        var parts = path.Split('.');
        object? current = null;

        // Start with context
        if (parts.Length > 0 && context.ContainsKey(parts[0]))
        {
            current = context[parts[0]];
            parts = parts.Skip(1).ToArray();
        }

        // Navigate through the path
        foreach (var part in parts)
        {
            if (current == null) break;

            // Check if part contains array indexing like "items[0]"
            var arrayMatch = Regex.Match(part, @"^(\w+)\[(\d+)\]$");
            if (arrayMatch.Success)
            {
                var propertyName = arrayMatch.Groups[1].Value;
                var index = int.Parse(arrayMatch.Groups[2].Value);

                // First get the property
                current = GetPropertyValue(current, propertyName);
                
                // Then access the array index
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

        if (obj is Dictionary<string, object?> dict)
        {
            return dict.ContainsKey(propertyName) ? dict[propertyName] : null;
        }
        else if (obj is JObject jobj)
        {
            return jobj[propertyName];
        }
        else if (obj is JToken jtoken)
        {
            return jtoken[propertyName];
        }
        else
        {
            // Try to get property via reflection
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj);
        }
    }

    private object? GetArrayElement(object? obj, int index)
    {
        if (obj == null) return null;

        if (obj is JArray jarray)
        {
            if (index >= 0 && index < jarray.Count)
                return jarray[index];
        }
        else if (obj is System.Collections.IList list)
        {
            if (index >= 0 && index < list.Count)
                return list[index];
        }
        else if (obj is Array array)
        {
            if (index >= 0 && index < array.Length)
                return array.GetValue(index);
        }

        return null;
    }

    private object? ConvertToNumber(object? value)
    {
        if (value == null) return 0;
        if (double.TryParse(value.ToString(), out var result))
            return result;
        return 0;
    }

    private string FormatDate(List<object?> args)
    {
        if (args.Count < 2) return DateTime.UtcNow.ToString();
        
        var dateValue = args[0];
        var format = args[1]?.ToString() ?? "yyyy-MM-dd";

        if (DateTime.TryParse(dateValue?.ToString(), out var date))
            return date.ToString(format);
        
        return DateTime.UtcNow.ToString(format);
    }

    private object? EvaluateIf(List<object?> args)
    {
        if (args.Count < 3) return null;
        
        var condition = args[0];
        var trueValue = args[1];
        var falseValue = args[2];

        var isTrue = condition != null && 
                    condition.ToString() != "false" && 
                    condition.ToString() != "0" && 
                    condition.ToString() != "";

        return isTrue ? trueValue : falseValue;
    }
}