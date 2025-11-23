using Microsoft.AspNetCore.Mvc;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Schedulers;

namespace TadbirRuleEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly ISchedulerService _schedulerService;

    public SchedulesController(ISchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PolicyScheduleDto>>> GetAll()
    {
        var schedules = await _schedulerService.GetSchedulesAsync();
        return Ok(schedules);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PolicyScheduleDto>> GetById(int id)
    {
        var schedule = await _schedulerService.GetScheduleByIdAsync(id);
        if (schedule == null)
            return NotFound();

        return Ok(schedule);
    }

    [HttpPost]
    public async Task<ActionResult<PolicyScheduleDto>> Create(CreatePolicyScheduleDto dto)
    {
        var schedule = await _schedulerService.CreateScheduleAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PolicyScheduleDto>> Update(int id, UpdatePolicyScheduleDto dto)
    {
        var schedule = await _schedulerService.UpdateScheduleAsync(id, dto);
        if (schedule == null)
            return NotFound();

        return Ok(schedule);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _schedulerService.DeleteScheduleAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartScheduler()
    {
        await _schedulerService.StartSchedulerAsync();
        return Ok(new { message = "Scheduler started" });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopScheduler()
    {
        await _schedulerService.StopSchedulerAsync();
        return Ok(new { message = "Scheduler stopped" });
    }
}