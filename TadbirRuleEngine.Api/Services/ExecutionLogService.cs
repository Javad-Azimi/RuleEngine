using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.DTOs;
using TadbirRuleEngine.Api.Models;

namespace TadbirRuleEngine.Api.Services;

public class ExecutionLogService : IExecutionLogService
{
    private readonly TadbirDbContext _context;
    private readonly ILogger<ExecutionLogService> _logger;

    public ExecutionLogService(TadbirDbContext context, ILogger<ExecutionLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ExecutionLogDto>> GetLogsAsync(int? policyId = null, int skip = 0, int take = 50)
    {
        var query = _context.ExecutionLogs
            .Include(e => e.Policy)
            .Include(e => e.Rule)
            .AsQueryable();

        if (policyId.HasValue)
        {
            query = query.Where(e => e.PolicyId == policyId.Value);
        }

        var logs = await query
            .OrderByDescending(e => e.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return logs.Select(MapToDto);
    }

    public async Task LogExecutionAsync(int policyId, int? ruleId, string status, 
        object? input, object? output, string? errorMessage, 
        DateTime startedAt, DateTime completedAt)
    {
        try
        {
            var log = new ExecutionLog
            {
                PolicyId = policyId,
                RuleId = ruleId,
                Status = status,
                Input = input != null ? JsonConvert.SerializeObject(input) : null,
                Output = output != null ? JsonConvert.SerializeObject(output) : null,
                ErrorMessage = errorMessage,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                DurationMs = (int)(completedAt - startedAt).TotalMilliseconds
            };

            _context.ExecutionLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging execution for policy {PolicyId}, rule {RuleId}", 
                policyId, ruleId);
        }
    }

    private static ExecutionLogDto MapToDto(ExecutionLog log)
    {
        return new ExecutionLogDto
        {
            Id = log.Id,
            PolicyId = log.PolicyId,
            RuleId = log.RuleId,
            Status = log.Status,
            Input = log.Input,
            Output = log.Output,
            ErrorMessage = log.ErrorMessage,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            DurationMs = log.DurationMs,
            PolicyName = log.Policy?.Name,
            RuleName = log.Rule?.Name
        };
    }
}