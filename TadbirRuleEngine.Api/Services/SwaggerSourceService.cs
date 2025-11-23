using Microsoft.EntityFrameworkCore;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Models;

namespace TadbirRuleEngine.Api.Services;

public class SwaggerSourceService : ISwaggerSourceService
{
    private readonly TadbirDbContext _context;
    private readonly IOpenApiParserService _parserService;
    private readonly ILogger<SwaggerSourceService> _logger;

    public SwaggerSourceService(
        TadbirDbContext context,
        IOpenApiParserService parserService,
        ILogger<SwaggerSourceService> logger)
    {
        _context = context;
        _parserService = parserService;
        _logger = logger;
    }

    public async Task<IEnumerable<SwaggerSourceDto>> GetAllAsync()
    {
        var sources = await _context.SwaggerSources
            .OrderBy(s => s.Name)
            .ToListAsync();

        return sources.Select(MapToDto);
    }

    public async Task<SwaggerSourceDto?> GetByIdAsync(int id)
    {
        var source = await _context.SwaggerSources.FindAsync(id);
        return source != null ? MapToDto(source) : null;
    }

    public async Task<SwaggerSourceDto> CreateAsync(CreateSwaggerSourceDto dto)
    {
        var source = new SwaggerSource
        {
            Name = dto.Name,
            SwaggerUrl = dto.SwaggerUrl,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.SwaggerSources.Add(source);
        await _context.SaveChangesAsync();

        return MapToDto(source);
    }

    public async Task<SwaggerSourceDto?> UpdateAsync(int id, UpdateSwaggerSourceDto dto)
    {
        var source = await _context.SwaggerSources.FindAsync(id);
        if (source == null) return null;

        source.Name = dto.Name;
        source.SwaggerUrl = dto.SwaggerUrl;
        source.Description = dto.Description;
        source.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return MapToDto(source);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var source = await _context.SwaggerSources.FindAsync(id);
        if (source == null) return false;

        _context.SwaggerSources.Remove(source);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SyncAsync(int id)
    {
        try
        {
            var source = await _context.SwaggerSources.FindAsync(id);
            if (source == null) return false;

            var apiDefinitions = await _parserService.ParseSwaggerAsync(source.SwaggerUrl);
            
            // Remove existing API definitions for this source
            var existingApis = await _context.ApiDefinitions
                .Where(a => a.SwaggerSourceId == id)
                .ToListAsync();
            _context.ApiDefinitions.RemoveRange(existingApis);

            // Add new API definitions
            foreach (var apiDto in apiDefinitions)
            {
                var apiDef = new ApiDefinition
                {
                    SwaggerSourceId = id,
                    Name = apiDto.Name,
                    Path = apiDto.Path,
                    Method = apiDto.Method,
                    Description = apiDto.Description,
                    RequestSchema = apiDto.RequestSchema,
                    ResponseSchema = apiDto.ResponseSchema,
                    Parameters = apiDto.Parameters,
                    RequiresAuth = apiDto.RequiresAuth
                };
                _context.ApiDefinitions.Add(apiDef);
            }

            source.LastSyncAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully synced {Count} APIs from {Source}", 
                apiDefinitions.Count(), source.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing swagger source {Id}", id);
            return false;
        }
    }

    private static SwaggerSourceDto MapToDto(SwaggerSource source)
    {
        return new SwaggerSourceDto
        {
            Id = source.Id,
            Name = source.Name,
            SwaggerUrl = source.SwaggerUrl,
            Description = source.Description,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            LastSyncAt = source.LastSyncAt
        };
    }
}