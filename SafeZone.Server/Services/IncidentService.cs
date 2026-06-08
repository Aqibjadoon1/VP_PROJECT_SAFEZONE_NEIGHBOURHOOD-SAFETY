using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public class IncidentService : IIncidentService
{
    private readonly SafeZoneDbContext _context;
    private readonly IGmailNotificationService? _gmail;
    private readonly ISlackNotificationService? _slack;
    private readonly ILogger<IncidentService>? _logger;

    public IncidentService(
        SafeZoneDbContext context,
        IGmailNotificationService? gmail = null,
        ISlackNotificationService? slack = null,
        ILogger<IncidentService>? logger = null)
    {
        _context = context;
        _gmail = gmail;
        _slack = slack;
        _logger = logger;
    }

    public async Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentDto dto, Guid? reporterId)
    {
        var category = await _context.IncidentCategories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId)
            ?? throw new InvalidOperationException("Invalid category ID");

        var incident = new Incident
        {
            IncidentId = Guid.NewGuid(),
            IncidentNumber = await GenerateIncidentNumberAsync(),
            CategoryId = dto.CategoryId,
            ReporterId = reporterId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address ?? string.Empty,
            Title = dto.Title,
            Description = dto.Description,
            Status = IncidentStatus.Pending,
            Severity = dto.Severity,
            IsAnonymous = dto.IsAnonymous,
            IsFIRFiled = false,
            EvidenceUrls = dto.EvidenceUrls,
            ReportedAt = DateTime.UtcNow,
            IncidentDateTime = dto.IncidentDateTime ?? DateTime.UtcNow,
            WitnessCount = dto.WitnessCount,
            SuspectDescription = dto.SuspectDescription,
            EstimatedLoss = dto.EstimatedLoss
        };

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        if (reporterId.HasValue && _gmail != null)
        {
            var reporter = await _context.Users.FindAsync(reporterId.Value);
            var recipientEmail = reporter?.Email ?? reporter?.UserName;
            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                try
                {
                    var sent = await _gmail.SendIncidentAlertAsync(recipientEmail, dto.Title, dto.Severity.ToString());
                    if (!sent)
                    {
                        _logger?.LogWarning("[Incident Notification] Gmail alert was not sent to {Email}. Check Gmail API configuration.", recipientEmail);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to send Gmail incident alert to {Email}.", recipientEmail);
                }
            }
            else
            {
                _logger?.LogWarning("[Incident Notification] Cannot send email alert: reporter {UserId} has no email address.", reporterId.Value);
            }
        }

        if (_slack != null && (dto.Severity == SeverityLevel.Critical || dto.Severity == SeverityLevel.High))
        {
            try
            {
                var sent = await _slack.SendAlertAsync(
                    dto.Title,
                    $"New {dto.Severity} severity incident at {dto.Address}: {dto.Description}",
                    dto.Severity.ToString());
                if (!sent)
                {
                    _logger?.LogWarning("[Incident Notification] Slack alert was not sent for incident '{Title}'. Check Slack webhook configuration.", dto.Title);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send Slack incident alert.");
            }
        }

        // Notify all superadmins
        try
        {
            var superAdminEmails = await _context.Users
                .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive)
                .Select(u => u.Email ?? u.UserName)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .ToListAsync();

            foreach (var adminEmail in superAdminEmails)
            {
                if (_gmail != null)
                {
                    await _gmail.SendEmailAsync(adminEmail!,
                        $"[{dto.Severity}] New Incident: {dto.Title}",
                        $"A new incident has been reported.\n\nTitle: {dto.Title}\nSeverity: {dto.Severity}\nLocation: {dto.Address ?? "N/A"}\nDescription: {dto.Description?[..Math.Min(dto.Description.Length, 300)]}\nReported: {incident.ReportedAt:MMM dd, yyyy HH:mm}\n\nView at: /authority/field-reports");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send superadmin incident notifications.");
        }

        return MapToResponse(incident, category);
    }

    public async Task<IncidentResponseDto?> GetIncidentByIdAsync(Guid incidentId)
    {
        var incident = await _context.Incidents
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Reporter)
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

        return incident == null ? null : MapToResponse(incident, incident.Category);
    }

    public async Task<List<IncidentListDto>> GetMyIncidentsAsync(Guid reporterId)
    {
        return await _context.Incidents
            .AsNoTracking()
            .Include(i => i.Category)
            .Where(i => i.ReporterId == reporterId)
            .OrderByDescending(i => i.ReportedAt)
            .Select(i => new IncidentListDto
            {
                IncidentId = i.IncidentId,
                IncidentNumber = i.IncidentNumber,
                CategoryName = i.Category != null ? i.Category.Name : "N/A",
                CategoryIcon = i.Category != null ? i.Category!.Icon : null,
                Title = i.Title,
                Description = i.Description,
                Status = i.Status,
                Severity = i.Severity,
                Address = i.Address,
                Latitude = i.Latitude,
                Longitude = i.Longitude,
                ReportedAt = i.ReportedAt,
                IncidentDateTime = i.IncidentDateTime
            })
            .ToListAsync();
    }

    public async Task<List<IncidentListDto>> GetAllIncidentsAsync(
        IncidentStatus? status = null,
        SeverityLevel? severity = null,
        Guid? categoryId = null)
    {
        var query = _context.Incidents
            .AsNoTracking()
            .Include(i => i.Category)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(i => i.Severity == severity.Value);

        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);

        return await query
            .OrderByDescending(i => i.ReportedAt)
            .Select(i => new IncidentListDto
            {
                IncidentId = i.IncidentId,
                IncidentNumber = i.IncidentNumber,
                CategoryName = i.Category != null ? i.Category.Name : "N/A",
                CategoryIcon = i.Category != null ? i.Category!.Icon : null,
                Title = i.Title,
                Description = i.Description,
                Status = i.Status,
                Severity = i.Severity,
                Address = i.Address,
                Latitude = i.Latitude,
                Longitude = i.Longitude,
                ReportedAt = i.ReportedAt,
                IncidentDateTime = i.IncidentDateTime
            })
            .ToListAsync();
    }

    public async Task<IncidentResponseDto?> UpdateIncidentAsync(Guid incidentId, UpdateIncidentDto dto, Guid? updatedBy)
    {
        var incident = await _context.Incidents
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

        if (incident == null) return null;

        if (!string.IsNullOrEmpty(dto.Title))
            incident.Title = dto.Title;

        if (!string.IsNullOrEmpty(dto.Description))
            incident.Description = dto.Description;

        if (dto.Severity.HasValue)
            incident.Severity = dto.Severity.Value;

        if (dto.Status.HasValue)
        {
            incident.Status = dto.Status.Value;
            if (dto.Status.Value == IncidentStatus.Resolved || dto.Status.Value == IncidentStatus.Closed)
            {
                incident.ResolvedAt = DateTime.UtcNow;
            }
        }

        if (dto.AssignedAuthorityId.HasValue)
            incident.AssignedAuthorityId = dto.AssignedAuthorityId.Value;

        await _context.SaveChangesAsync();
        return MapToResponse(incident, incident.Category);
    }

    public async Task<IncidentResponseDto?> UpdateStatusAsync(Guid incidentId, IncidentStatus status, Guid? updatedBy)
    {
        var incident = await _context.Incidents
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

        if (incident == null) return null;

        incident.Status = status;
        if (status == IncidentStatus.Resolved || status == IncidentStatus.Closed)
        {
            incident.ResolvedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return MapToResponse(incident, incident.Category);
    }

    public async Task<IncidentResponseDto?> AssignAuthorityAsync(Guid incidentId, Guid authorityId)
    {
        var incident = await _context.Incidents
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

        if (incident == null) return null;

        incident.AssignedAuthorityId = authorityId;
        incident.Status = IncidentStatus.Assigned;

        await _context.SaveChangesAsync();
        return MapToResponse(incident, incident.Category);
    }

    public async Task<List<MapIncidentDto>> GetIncidentsForMapAsync(
        double? minLat = null, double? maxLat = null,
        double? minLng = null, double? maxLng = null,
        IncidentStatus? status = null)
    {
        var query = _context.Incidents
            .AsNoTracking()
            .Include(i => i.Category)
            .AsQueryable();

        if (minLat.HasValue)
            query = query.Where(i => i.Latitude >= minLat.Value);

        if (maxLat.HasValue)
            query = query.Where(i => i.Latitude <= maxLat.Value);

        if (minLng.HasValue)
            query = query.Where(i => i.Longitude >= minLng.Value);

        if (maxLng.HasValue)
            query = query.Where(i => i.Longitude <= maxLng.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        else
            query = query.Where(i => i.Status != IncidentStatus.Closed);

        return await query
            .Select(i => new MapIncidentDto
            {
                IncidentId = i.IncidentId,
                IncidentNumber = i.IncidentNumber,
                Lat = i.Latitude,
                Lng = i.Longitude,
                Title = i.Title,
                CategoryName = i.Category != null ? i.Category.Name : "N/A",
                CategoryIcon = i.Category != null ? i.Category!.Icon : null,
                CategoryColor = i.Category != null ? i.Category.Color : null,
                Status = i.Status,
                Severity = i.Severity,
                ReportedAt = i.ReportedAt
            })
            .ToListAsync();
    }

    public async Task<List<HeatmapPointDto>> GetHeatmapDataAsync(DateTime? since = null)
    {
        var query = _context.Incidents
            .AsNoTracking()
            .Where(i => i.Status != IncidentStatus.Closed)
            .AsQueryable();

        if (since.HasValue)
            query = query.Where(i => i.ReportedAt >= since.Value);

        var last7Days = DateTime.UtcNow.AddDays(-7);

        return await query
            .Select(i => new HeatmapPointDto
            {
                Lat = i.Latitude,
                Lng = i.Longitude,
                Intensity = i.ReportedAt >= last7Days ? 1.0 : 0.5,
                Severity = i.Severity
            })
            .ToListAsync();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await _context.IncidentCategories
            .AsNoTracking()
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Icon = c.Icon,
                Color = c.Color,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task<int> GetIncidentCountByStatusAsync(IncidentStatus status)
    {
        return await _context.Incidents
            .AsNoTracking()
            .CountAsync(i => i.Status == status);
    }

    public async Task<Dictionary<SeverityLevel, int>> GetIncidentCountBySeverityAsync()
    {
        return await _context.Incidents
            .AsNoTracking()
            .Where(i => i.Status != IncidentStatus.Closed)
            .GroupBy(i => i.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count);
    }

    private static string GenerateIncidentNumber()
    {
        var timestamp = DateTime.UtcNow;
        var random = Random.Shared.Next(1000, 9999);
        return $"INC-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{random}";
    }

    private async Task<string> GenerateIncidentNumberAsync()
    {
        var timestamp = DateTime.UtcNow;
        var random = Random.Shared.Next(1000, 9999);
        var number = $"INC-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{random}";

        // Ensure uniqueness by checking database
        var exists = await _context.Incidents.AnyAsync(i => i.IncidentNumber == number);
        if (exists)
        {
            random = Random.Shared.Next(1000, 9999);
            number = $"INC-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{random}";
        }

        return number;
    }

    private IncidentResponseDto MapToResponse(Incident incident, IncidentCategory? category)
    {
        return new IncidentResponseDto
        {
            IncidentId = incident.IncidentId,
            IncidentNumber = incident.IncidentNumber,
            CategoryId = incident.CategoryId,
            CategoryName = category?.Name ?? "N/A",
            CategoryIcon = category?.Icon,
            CategoryColor = category?.Color,
            ReporterId = incident.ReporterId,
            ReporterName = incident.IsAnonymous ? "Anonymous" : incident.Reporter?.FullName,
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            Address = incident.Address,
            Title = incident.Title,
            Description = incident.Description,
            Status = incident.Status,
            Severity = incident.Severity,
            IsAnonymous = incident.IsAnonymous,
            IsFIRFiled = incident.IsFIRFiled,
            ReportedAt = incident.ReportedAt,
            IncidentDateTime = incident.IncidentDateTime,
            ResolvedAt = incident.ResolvedAt,
            AssignedAuthorityId = incident.AssignedAuthorityId,
            WitnessCount = incident.WitnessCount,
            EstimatedLoss = incident.EstimatedLoss
        };
    }
}
