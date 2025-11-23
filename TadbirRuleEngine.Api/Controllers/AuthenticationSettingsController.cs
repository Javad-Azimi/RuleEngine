using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Models;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationSettingsController : ControllerBase
{
    private readonly TadbirDbContext _context;
    private readonly ILogger<AuthenticationSettingsController> _logger;

    public AuthenticationSettingsController(
        TadbirDbContext context,
        ILogger<AuthenticationSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthenticationSettingDto>>> GetAll()
    {
        var settings = await _context.AuthenticationSettings
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(settings.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuthenticationSettingDto>> GetById(int id)
    {
        var setting = await _context.AuthenticationSettings.FindAsync(id);
        if (setting == null)
            return NotFound();

        return Ok(MapToDto(setting));
    }

    [HttpPost]
    public async Task<ActionResult<AuthenticationSettingDto>> Create(CreateAuthenticationSettingDto dto)
    {
        var setting = new AuthenticationSetting
        {
            Name = dto.Name,
            TokenEndpoint = dto.TokenEndpoint,
            Username = dto.Username,
            Password = dto.Password,
            GrantType = dto.GrantType,
            ClientId = dto.ClientId,
            ClientSecret = dto.ClientSecret,
            Scope = dto.Scope,
            AdditionalParameters = dto.AdditionalParameters,
            IsActive = dto.IsActive
        };

        _context.AuthenticationSettings.Add(setting);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = setting.Id }, MapToDto(setting));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AuthenticationSettingDto>> Update(int id, UpdateAuthenticationSettingDto dto)
    {
        var setting = await _context.AuthenticationSettings.FindAsync(id);
        if (setting == null)
            return NotFound();

        setting.Name = dto.Name;
        setting.TokenEndpoint = dto.TokenEndpoint;
        setting.Username = dto.Username;
        
        if (!string.IsNullOrEmpty(dto.Password))
            setting.Password = dto.Password;
        
        setting.GrantType = dto.GrantType;
        setting.ClientId = dto.ClientId;
        
        if (!string.IsNullOrEmpty(dto.ClientSecret))
            setting.ClientSecret = dto.ClientSecret;
        
        setting.Scope = dto.Scope;
        setting.AdditionalParameters = dto.AdditionalParameters;
        setting.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(setting));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var setting = await _context.AuthenticationSettings.FindAsync(id);
        if (setting == null)
            return NotFound();

        _context.AuthenticationSettings.Remove(setting);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static AuthenticationSettingDto MapToDto(AuthenticationSetting setting)
    {
        return new AuthenticationSettingDto
        {
            Id = setting.Id,
            Name = setting.Name,
            TokenEndpoint = setting.TokenEndpoint,
            Username = setting.Username,
            GrantType = setting.GrantType,
            ClientId = setting.ClientId,
            Scope = setting.Scope,
            IsActive = setting.IsActive,
            CreatedAt = setting.CreatedAt,
            LastUsedAt = setting.LastUsedAt
        };
    }
}