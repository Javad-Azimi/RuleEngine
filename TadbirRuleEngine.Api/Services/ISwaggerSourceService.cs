using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Services;

public interface ISwaggerSourceService
{
    Task<IEnumerable<SwaggerSourceDto>> GetAllAsync();
    Task<SwaggerSourceDto?> GetByIdAsync(int id);
    Task<SwaggerSourceDto> CreateAsync(CreateSwaggerSourceDto dto);
    Task<SwaggerSourceDto?> UpdateAsync(int id, UpdateSwaggerSourceDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> SyncAsync(int id);
}