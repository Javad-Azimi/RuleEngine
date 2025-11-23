namespace TadbirRuleEngine.Api.Mapping;

public interface IMappingService
{
    Task<object?> ApplyMappingAsync(object? source, object? mappingDefinition, Dictionary<string, object?>? context = null);
    Task<string> RenderTemplateAsync(string template, Dictionary<string, object?> context);
    object? EvaluateExpression(string expression, Dictionary<string, object?> context);
}