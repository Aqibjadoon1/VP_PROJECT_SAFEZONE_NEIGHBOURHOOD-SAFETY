using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.Models;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Authority,Admin,SuperAdmin")]
public class AnalyticsController : ControllerBase
{
    private readonly SafeZoneDbContext _context;

    public AnalyticsController(SafeZoneDbContext context)
    {
        _context = context;
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 7)
    {
        if (days < 1 || days > 365)
            return BadRequest(new { message = "Days must be between 1 and 365." });

        var since = DateTime.UtcNow.AddDays(-days);

        var dailyCounts = await _context.Incidents
            .AsNoTracking()
            .Where(i => i.ReportedAt >= since)
            .GroupBy(i => i.ReportedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                count = g.Count(),
                critical = g.Count(i => i.Severity == SeverityLevel.Critical),
                high = g.Count(i => i.Severity == SeverityLevel.High),
                medium = g.Count(i => i.Severity == SeverityLevel.Medium),
                low = g.Count(i => i.Severity == SeverityLevel.Low)
            })
            .ToListAsync();

        var totalIncidents = dailyCounts.Sum(d => d.count);
        var resolved = await _context.Incidents
            .AsNoTracking()
            .CountAsync(i => i.ReportedAt >= since && (i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.Closed));
        var resolutionRate = totalIncidents > 0 ? Math.Round((double)resolved / totalIncidents * 100, 1) : 0;

        return Ok(new { days, since = since.ToString("O"), totalIncidents, resolved, resolutionRate, dailyCounts });
    }

    [HttpGet("severity-distribution")]
    public async Task<IActionResult> GetSeverityDistribution()
    {
        var totalIncidents = await _context.Incidents.AsNoTracking().CountAsync();

        var distribution = await _context.Incidents
            .AsNoTracking()
            .GroupBy(i => i.Severity)
            .Select(g => new
            {
                severity = g.Key.ToString(),
                count = g.Count(),
                percentage = totalIncidents > 0
                    ? Math.Round((double)g.Count() / totalIncidents * 100, 1)
                    : 0.0
            })
            .ToListAsync();

        return Ok(new { totalIncidents, distribution });
    }

    [HttpGet("response-times")]
    public async Task<IActionResult> GetResponseTimes()
    {
        var responseStats = await _context.Incidents
            .AsNoTracking()
            .Where(i => i.ResolvedAt != null && i.ReportedAt != default)
            .GroupBy(i => i.Severity)
            .Select(g => new
            {
                severity = g.Key.ToString(),
                count = g.Count(),
                avgHours = Math.Round(g.Average(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalHours), 1),
                minHours = Math.Round(g.Min(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalHours), 1),
                maxHours = Math.Round(g.Max(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalHours), 1)
            })
            .ToListAsync();

        var totalResolved = responseStats.Sum(r => r.count);
        var avgResponseHours = totalResolved > 0
            ? Math.Round(responseStats.Sum(r => r.avgHours * r.count) / totalResolved, 1)
            : 0;
        var minResponseHours = totalResolved > 0 ? responseStats.Min(r => r.minHours) : 0;
        var maxResponseHours = totalResolved > 0 ? responseStats.Max(r => r.maxHours) : 0;

        return Ok(new
        {
            totalResolved,
            avgResponseHours,
            minResponseHours,
            maxResponseHours,
            bySeverity = responseStats
        });
    }
}
