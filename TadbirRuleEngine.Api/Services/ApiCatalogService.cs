using Microsoft.EntityFrameworkCore;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Services;

public class ApiCatalogService : IApiCatalogService
{
    private readonly TadbirDbContext _context;

    public ApiCatalogService(TadbirDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApiDefinitionDto>> GetAllAsync()
    {
        var apis = await _context.ApiDefinitions
            .Include(a => a.SwaggerSource)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return apis.Select(MapToDto);
    }

    public async Task<IEnumerable<ApiDefinitionDto>> GetBySwaggerSourceIdAsync(int swaggerSourceId)
    {
        var apis = await _context.ApiDefinitions
            .Include(a => a.SwaggerSource)
            .Where(a => a.SwaggerSourceId == swaggerSourceId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return apis.Select(MapToDto);
    }

    public async Task<ApiDefinitionDto?> GetByIdAsync(int id)
    {
        var api = await _context.ApiDefinitions
            .Include(a => a.SwaggerSource)
            .FirstOrDefaultAsync(a => a.Id == id);

        return api != null ? MapToDto(api) : null;
    }

    private static ApiDefinitionDto MapToDto(Models.ApiDefinition api)
    {
        return new ApiDefinitionDto
        {
            Id = api.Id,
            SwaggerSourceId = api.SwaggerSourceId,
            Name = api.Name,
            Path = api.Path,
            Method = api.Method,
            Description = api.Description,
            RequestSchema = api.RequestSchema,
            ResponseSchema = api.ResponseSchema,
            Parameters = api.Parameters,
            RequiresAuth = api.RequiresAuth,
            CreatedAt = api.CreatedAt,
            SwaggerSource = api.SwaggerSource != null ? new SwaggerSourceDto
            {
                Id = api.SwaggerSource.Id,
                Name = api.SwaggerSource.Name,
                SwaggerUrl = api.SwaggerSource.SwaggerUrl,
                Description = api.SwaggerSource.Description,
                IsActive = api.SwaggerSource.IsActive,
                CreatedAt = api.SwaggerSource.CreatedAt,
                LastSyncAt = api.SwaggerSource.LastSyncAt
            } : null
        };
    }
}