using Microsoft.AspNetCore.Mvc;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Rules;
using TadbirRuleEngine.Api.Services;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IRuleEngineService _ruleEngineService;
    private readonly IAuthTokenService _authTokenService;

    public RulesController(IRuleEngineService ruleEngineService, IAuthTokenService authTokenService)
    {
        _ruleEngineService = ruleEngineService;
        _authTokenService = authTokenService;
    }

    [HttpGet("by-policy/{policyId}")]
    public async Task<ActionResult<IEnumerable<RuleDto>>> GetByPolicyId(int policyId)
    {
        var rules = await _ruleEngineService.GetRulesByPolicyIdAsync(policyId);
        return Ok(rules);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RuleDto>> GetById(int id)
    {
        var rule = await _ruleEngineService.GetRuleByIdAsync(id);
        if (rule == null)
            return NotFound();

        return Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<RuleDto>> Create(CreateRuleDto dto)
    {
        var rule = await _ruleEngineService.CreateRuleAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RuleDto>> Update(int id, UpdateRuleDto dto)
    {
        var rule = await _ruleEngineService.UpdateRuleAsync(id, dto);
        if (rule == null)
            return NotFound();

        return Ok(rule);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _ruleEngineService.DeleteRuleAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/test-condition")]
    public async Task<ActionResult<bool>> TestCondition(int id, [FromBody] Dictionary<string, object?> context)
    {
        var result = await _ruleEngineService.EvaluateRuleConditionAsync(id, context);
        return Ok(result);
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<object>> Execute(int id, [FromBody] Dictionary<string, object?> context)
    {
        var result = await _ruleEngineService.ExecuteRuleActionAsync(id, context);
        return Ok(result);
    }

    [HttpPost("test")]
    public async Task<ActionResult<object>> TestApiCall([FromBody] TestApiCallRequest request)
    {
        try
        {
            var context = new Dictionary<string, object?>();
            
            // Use previousResult from request
            if (request.PreviousResult != null)
            {
                context["previousResult"] = request.PreviousResult;
            }

            // If input mapping is provided, store it in context
            if (request.InputMapping != null)
            {
                context["inputMapping"] = request.InputMapping;
            }

            // If auth setting is provided, acquire token
            if (request.AuthSettingId.HasValue)
            {
                var token = await _authTokenService.AcquireTokenAsync(request.AuthSettingId.Value);
                if (!string.IsNullOrEmpty(token))
                {
                    context["authToken"] = token;
                }
            }

            // Execute API call directly by API ID (not rule ID)
            var result = await _ruleEngineService.ExecuteApiCallDirectAsync(request.ApiId, context);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class TestApiCallRequest
{
    public int ApiId { get; set; }
    public int? AuthSettingId { get; set; }
    public object? PreviousResult { get; set; }
    public object? InputMapping { get; set; }
}
