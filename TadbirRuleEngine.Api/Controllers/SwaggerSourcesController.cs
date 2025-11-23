using Microsoft.AspNetCore.Mvc;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Services;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SwaggerSourcesController : ControllerBase
{
    private readonly ISwaggerSourceService _swaggerSourceService;

    public SwaggerSourcesController(ISwaggerSourceService swaggerSourceService)
    {
        _swaggerSourceService = swaggerSourceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SwaggerSourceDto>>> GetAll()
    {
        var sources = await _swaggerSourceService.GetAllAsync();
        return Ok(sources);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SwaggerSourceDto>> GetById(int id)
    {
        var source = await _swaggerSourceService.GetByIdAsync(id);
        if (source == null)
            return NotFound();

        return Ok(source);
    }

    [HttpPost]
    public async Task<ActionResult<SwaggerSourceDto>> Create(CreateSwaggerSourceDto dto)
    {
        var source = await _swaggerSourceService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = source.Id }, source);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SwaggerSourceDto>> Update(int id, UpdateSwaggerSourceDto dto)
    {
        var source = await _swaggerSourceService.UpdateAsync(id, dto);
        if (source == null)
            return NotFound();

        return Ok(source);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _swaggerSourceService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/sync")]
    public async Task<IActionResult> Sync(int id)
    {
        var result = await _swaggerSourceService.SyncAsync(id);
        if (!result)
            return NotFound();

        return Ok(new { message = "Sync completed successfully" });
    }
}