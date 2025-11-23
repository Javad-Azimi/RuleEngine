using Microsoft.AspNetCore.Mvc;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Services;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyExecutorService _policyExecutorService;

    public PoliciesController(IPolicyExecutorService policyExecutorService)
    {
        _policyExecutorService = policyExecutorService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PolicyDto>>> GetAll()
    {
        var policies = await _policyExecutorService.GetAllPoliciesAsync();
        return Ok(policies);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PolicyDto>> GetById(int id)
    {
        var policy = await _policyExecutorService.GetPolicyByIdAsync(id);
        if (policy == null)
            return NotFound();

        return Ok(policy);
    }

    [HttpPost]
    public async Task<ActionResult<PolicyDto>> Create(CreatePolicyDto dto)
    {
        var policy = await _policyExecutorService.CreatePolicyAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PolicyDto>> Update(int id, UpdatePolicyDto dto)
    {
        var policy = await _policyExecutorService.UpdatePolicyAsync(id, dto);
        if (policy == null)
            return NotFound();

        return Ok(policy);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _policyExecutorService.DeletePolicyAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<object>> Execute(int id, [FromBody] Dictionary<string, object?>? context = null)
    {
        var result = await _policyExecutorService.ExecutePolicyAsync(id, context);
        return Ok(new { result });
    }
}