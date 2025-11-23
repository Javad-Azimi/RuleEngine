using Microsoft.AspNetCore.Mvc;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Services;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExecutionLogsController : ControllerBase
{
    private readonly IExecutionLogService _executionLogService;

    public ExecutionLogsController(IExecutionLogService executionLogService)
    {
        _executionLogService = executionLogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExecutionLogDto>>> GetLogs(
        [FromQuery] int? policyId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var logs = await _executionLogService.GetLogsAsync(policyId, skip, take);
        return Ok(logs);
    }
}