using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public class IncidentService : IIncidentService
{
    private readonly SafeZoneDbContext _context;

    public IncidentService(SafeZoneDbContext context)
    {
        _context = context;
    }

    public async Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentDto dto, Guid? reporterId)
    {
        var category = await _context.IncidentCategories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId)
            ?? throw new InvalidOperationException("Invalid category ID");

        var incident = new Incident
        {
            IncidentId = Guid.NewGuid(),
            IncidentNumber = GenerateIncidentNumber(),
            CategoryId = dto.CategoryId,
            ReporterId = dto.IsAnonymous ? null : reporterId,
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

        return MapToResponse(incident, category);
    }

    public async Task<IncidentResponseDto?> GetIncidentByIdAsync(Guid incidentId)
    {
        var incident = await _context.Incidents
            .Include(i => i.Category)
            .Include(i => i.Reporter)
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

        return incident == null ? null : MapToResponse(incident, incident.Category);
    }

     public async Task<List<IncidentListDto>> GetMyIncidentsAsync(Guid reporterId)
    {
        return await _context.Incidents
            .Include(i => i.Category)
            .Where(i => i.ReporterId == reporterId)
            .OrderByDescending(i => i.ReportedAt)
            .Select(i => new IncidentListDto
            {
                IncidentId = i.IncidentId,
                IncidentNumber = i.IncidentNumber,
                CategoryName = i.Category.Name,
                CategoryIcon = i.Category.Icon,
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
                CategoryName = i.Category.Name,
                CategoryIcon = i.Category.Icon,
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
                CategoryName = i.Category.Name,
                CategoryIcon = i.Category.Icon,
                CategoryColor = i.Category.Color,
                Status = i.Status,
                Severity = i.Severity,
                ReportedAt = i.ReportedAt
            })
            .ToListAsync();
    }

    public async Task<List<HeatmapPointDto>> GetHeatmapDataAsync(DateTime? since = null)
    {
        var query = _context.Incidents
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
            .CountAsync(i => i.Status == status);
    }

    public async Task<Dictionary<SeverityLevel, int>> GetIncidentCountBySeverityAsync()
    {
        return await _context.Incidents
            .Where(i => i.Status != IncidentStatus.Closed)
            .GroupBy(i => i.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count);
    }

    private string GenerateIncidentNumber()
    {
        var timestamp = DateTime.UtcNow;
        var random = new Random().Next(1000, 9999);
        return $"INC-{timestamp:yyyyMMdd}-{random}";
    }

    private IncidentResponseDto MapToResponse(Incident incident, IncidentCategory category)
    {
        return new IncidentResponseDto
        {
            IncidentId = incident.IncidentId,
            IncidentNumber = incident.IncidentNumber,
            CategoryId = incident.CategoryId,
            CategoryName = category.Name,
            CategoryIcon = category.Icon,
            CategoryColor = category.Color,
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
