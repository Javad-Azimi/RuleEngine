using Microsoft.AspNetCore.Mvc;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Services;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiCatalogController : ControllerBase
{
    private readonly IApiCatalogService _apiCatalogService;

    public ApiCatalogController(IApiCatalogService apiCatalogService)
    {
        _apiCatalogService = apiCatalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiDefinitionDto>>> GetAll()
    {
        var apis = await _apiCatalogService.GetAllAsync();
        return Ok(apis);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiDefinitionDto>> GetById(int id)
    {
        var api = await _apiCatalogService.GetByIdAsync(id);
        if (api == null)
            return NotFound();

        return Ok(api);
    }

    [HttpGet("by-source/{swaggerSourceId}")]
    public async Task<ActionResult<IEnumerable<ApiDefinitionDto>>> GetBySwaggerSourceId(int swaggerSourceId)
    {
        var apis = await _apiCatalogService.GetBySwaggerSourceIdAsync(swaggerSourceId);
        return Ok(apis);
    }
}