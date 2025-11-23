using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Services;

public interface IOpenApiParserService
{
    Task<IEnumerable<ApiDefinitionDto>> ParseSwaggerAsync(string swaggerUrl);
}