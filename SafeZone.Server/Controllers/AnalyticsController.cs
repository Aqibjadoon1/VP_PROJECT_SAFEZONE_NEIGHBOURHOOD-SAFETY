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
        var since = DateTime.UtcNow.AddDays(-days);
        var incidents = await _context.Incidents
            .Where(i => i.ReportedAt >= since)
            .ToListAsync();

        var dailyCounts = incidents
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
            });

        var totalIncidents = incidents.Count;
        var resolved = incidents.Count(i => i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.Closed);
        var resolutionRate = totalIncidents > 0 ? Math.Round((double)resolved / totalIncidents * 100, 1) : 0;

        return Ok(new { days, since = since.ToString("O"), totalIncidents, resolved, resolutionRate, dailyCounts });
    }

    [HttpGet("severity-distribution")]
    public async Task<IActionResult> GetSeverityDistribution()
    {
        var incidents = await _context.Incidents.ToListAsync();
        var distribution = Enum.GetValues<SeverityLevel>().Select(level => new
        {
            severity = level.ToString(),
            count = incidents.Count(i => i.Severity == level),
            percentage = incidents.Count > 0
                ? Math.Round((double)incidents.Count(i => i.Severity == level) / incidents.Count * 100, 1)
                : 0.0
        });
        return Ok(new { totalIncidents = incidents.Count, distribution });
    }

    [HttpGet("response-times")]
    public async Task<IActionResult> GetResponseTimes()
    {
        var resolved = await _context.Incidents
            .Where(i => i.ResolvedAt != null && i.ReportedAt != default)
            .ToListAsync();

        var responseTimes = resolved
            .Select(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalHours)
            .ToList();

        var bySeverity = resolved
            .GroupBy(i => i.Severity)
            .Select(g => new
            {
                severity = g.Key.ToString(),
                count = g.Count(),
                avgHours = Math.Round(g.Average(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalHours), 1),
                minHours = Math.Round(g.Min(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalHours), 1),
                maxHours = Math.Round(g.Max(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalHours), 1)
            });

        return Ok(new
        {
            totalResolved = resolved.Count,
            avgResponseHours = responseTimes.Count > 0 ? Math.Round(responseTimes.Average(), 1) : 0,
            minResponseHours = responseTimes.Count > 0 ? Math.Round(responseTimes.Min(), 1) : 0,
            maxResponseHours = responseTimes.Count > 0 ? Math.Round(responseTimes.Max(), 1) : 0,
            bySeverity
        });
    }
}
