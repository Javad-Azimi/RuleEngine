using Cronos;
using Microsoft.EntityFrameworkCore;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Models;
using TadbirRuleEngine.Api.Services;

namespace TadbirRuleEngine.Api.Schedulers;

public class SchedulerService : ISchedulerService, IDisposable
{
    private readonly TadbirDbContext _context;
    private readonly IPolicyExecutorService _policyExecutorService;
    private readonly ILogger<SchedulerService> _logger;
    private readonly Timer _timer;
    private bool _isRunning = false;

    public SchedulerService(
        TadbirDbContext context,
        IPolicyExecutorService policyExecutorService,
        ILogger<SchedulerService> logger)
    {
        _context = context;
        _policyExecutorService = policyExecutorService;
        _logger = logger;
        
        // Check every minute for scheduled tasks
        _timer = new Timer(CheckScheduledTasks, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<IEnumerable<PolicyScheduleDto>> GetSchedulesAsync()
    {
        var schedules = await _context.PolicySchedules
            .Include(s => s.Policy)
            .OrderBy(s => s.NextRunAt)
            .ToListAsync();

        return schedules.Select(MapToDto);
    }

    public async Task<PolicyScheduleDto?> GetScheduleByIdAsync(int id)
    {
        var schedule = await _context.PolicySchedules
            .Include(s => s.Policy)
            .FirstOrDefaultAsync(s => s.Id == id);

        return schedule != null ? MapToDto(schedule) : null;
    }

    public async Task<PolicyScheduleDto> CreateScheduleAsync(CreatePolicyScheduleDto dto)
    {
        var schedule = new PolicySchedule
        {
            PolicyId = dto.PolicyId,
            CronExpression = dto.CronExpression,
            IsActive = dto.IsActive,
            NextRunAt = CalculateNextRun(dto.CronExpression)
        };

        _context.PolicySchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Reload with includes
        schedule = await _context.PolicySchedules
            .Include(s => s.Policy)
            .FirstAsync(s => s.Id == schedule.Id);

        return MapToDto(schedule);
    }

    public async Task<PolicyScheduleDto?> UpdateScheduleAsync(int id, UpdatePolicyScheduleDto dto)
    {
        var schedule = await _context.PolicySchedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.CronExpression = dto.CronExpression;
        schedule.IsActive = dto.IsActive;
        schedule.NextRunAt = CalculateNextRun(dto.CronExpression);

        await _context.SaveChangesAsync();

        // Reload with includes
        schedule = await _context.PolicySchedules
            .Include(s => s.Policy)
            .FirstAsync(s => s.Id == schedule.Id);

        return MapToDto(schedule);
    }

    public async Task<bool> DeleteScheduleAsync(int id)
    {
        var schedule = await _context.PolicySchedules.FindAsync(id);
        if (schedule == null) return false;

        _context.PolicySchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task StartSchedulerAsync()
    {
        _isRunning = true;
        _logger.LogInformation("Scheduler started");
        await Task.CompletedTask;
    }

    public async Task StopSchedulerAsync()
    {
        _isRunning = false;
        _logger.LogInformation("Scheduler stopped");
        await Task.CompletedTask;
    }

    private async void CheckScheduledTasks(object? state)
    {
        if (!_isRunning) return;

        try
        {
            var now = DateTime.UtcNow;
            var dueSchedules = await _context.PolicySchedules
                .Include(s => s.Policy)
                .Where(s => s.IsActive && s.NextRunAt <= now)
                .ToListAsync();

            foreach (var schedule in dueSchedules)
            {
                try
                {
                    _logger.LogInformation("Executing scheduled policy {PolicyId}: {PolicyName}", 
                        schedule.PolicyId, schedule.Policy.Name);

                    await _policyExecutorService.ExecutePolicyAsync(schedule.PolicyId);

                    // Update schedule
                    schedule.LastRunAt = now;
                    schedule.NextRunAt = CalculateNextRun(schedule.CronExpression);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Scheduled policy {PolicyId} executed successfully", 
                        schedule.PolicyId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing scheduled policy {PolicyId}", 
                        schedule.PolicyId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking scheduled tasks");
        }
    }

    private DateTime? CalculateNextRun(string cronExpression)
    {
        try
        {
            var cron = CronExpression.Parse(cronExpression);
            return cron.GetNextOccurrence(DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid cron expression: {CronExpression}", cronExpression);
            return null;
        }
    }

    private static PolicyScheduleDto MapToDto(PolicySchedule schedule)
    {
        return new PolicyScheduleDto
        {
            Id = schedule.Id,
            PolicyId = schedule.PolicyId,
            CronExpression = schedule.CronExpression,
            IsActive = schedule.IsActive,
            NextRunAt = schedule.NextRunAt,
            LastRunAt = schedule.LastRunAt,
            CreatedAt = schedule.CreatedAt,
            PolicyName = schedule.Policy?.Name
        };
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}