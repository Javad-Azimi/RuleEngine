using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Services;

public interface IApiCatalogService
{
    Task<IEnumerable<ApiDefinitionDto>> GetAllAsync();
    Task<IEnumerable<ApiDefinitionDto>> GetBySwaggerSourceIdAsync(int swaggerSourceId);
    Task<ApiDefinitionDto?> GetByIdAsync(int id);
}