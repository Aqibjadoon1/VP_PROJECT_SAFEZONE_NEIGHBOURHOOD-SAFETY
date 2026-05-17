# Phase A: Incident System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the core incident reporting system with Leaflet.js map integration, 4-step report form, incident service, and real-time SignalR broadcasts.

**Architecture:** 
- Server: ASP.NET Core Web API with Clean Architecture patterns (Controllers → Services → DbContext)
- Client: Pure HTML + JS (Tailwind CDN, Leaflet.js CDN, SignalR CDN)
- Real-time: SignalR MapHub broadcasts new incidents to all connected clients

**Tech Stack:**
- ASP.NET Core 8, EF Core 8, SQL Server LocalDB
- SignalR (already configured)
- Leaflet.js v1.9.4 (CDN)
- Tailwind CSS v3 (CDN)

---

## File Structure & Changes

### New Files to Create

**Server:**
- `DTOs/IncidentDtos.cs` - CreateIncidentDto, UpdateIncidentDto, IncidentResponseDto, IncidentListDto, MapIncidentDto, HeatmapPointDto, CategoryDto
- `Services/IIncidentService.cs` - Service interface
- `Services/IncidentService.cs` - Service implementation
- `Controllers/IncidentController.cs` - Incident CRUD API
- `Controllers/MapController.cs` - Map/heatmap endpoints
- `Helpers/GeoHelper.cs` - Distance calculations, coordinate validation

**Client:**
- `js/map.js` - Leaflet.js wrapper, marker management, heatmap
- `js/geolocation.js` - Browser geolocation API wrapper

### Files to Modify

**Server:**
- `Program.cs` - Register IIncidentService, MapController CORS if needed
- `Data/SafeZoneDbContext.cs` - Ensure IncidentCategory is seeded and accessible

**Client:**
- `js/api.js` - Add incident/map endpoint methods
- `user/dashboard.html` - Integrate Leaflet map, remove "Coming Soon" placeholder
- `user/report-incident.html` - Replace placeholder with 4-step wizard
- `user/my-incidents.html` - Replace placeholder with actual incident list
- All HTML pages - Add Leaflet CSS/JS CDNs where maps are used

---

## Task 1: Incident DTOs

**Files:**
- Create: `SafeZone.Server/DTOs/IncidentDtos.cs`

- [ ] **Step 1: Write DTO file**

```csharp
using System.ComponentModel.DataAnnotations;
using SafeZone.Server.Models;

namespace SafeZone.Server.DTOs;

public record CreateIncidentDto
{
    [Required]
    public Guid CategoryId { get; init; }

    [Required]
    [Range(-90.0, 90.0)]
    public double Latitude { get; init; }

    [Required]
    [Range(-180.0, 180.0)]
    public double Longitude { get; init; }

    [MaxLength(200)]
    public string? Address { get; init; }

    [Required]
    [MaxLength(100)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;

    public SeverityLevel Severity { get; init; } = SeverityLevel.Medium;

    public bool IsAnonymous { get; init; } = false;

    public DateTime? IncidentDateTime { get; init; }

    public string? EvidenceUrls { get; init; }

    public int? WitnessCount { get; init; }

    [MaxLength(500)]
    public string? SuspectDescription { get; init; }

    public decimal? EstimatedLoss { get; init; }
}

public record UpdateIncidentDto
{
    [MaxLength(100)]
    public string? Title { get; init; }

    public string? Description { get; init; }

    public SeverityLevel? Severity { get; init; }

    public IncidentStatus? Status { get; init; }

    public Guid? AssignedAuthorityId { get; init; }

    [MaxLength(500)]
    public string? ResolutionNotes { get; init; }
}

public record IncidentResponseDto
{
    public Guid IncidentId { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryIcon { get; init; } = string.Empty;
    public string CategoryColor { get; init; } = string.Empty;
    public Guid? ReporterId { get; init; }
    public string? ReporterName { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IncidentStatus Status { get; init; }
    public SeverityLevel Severity { get; init; }
    public bool IsAnonymous { get; init; }
    public bool IsFIRFiled { get; init; }
    public DateTime ReportedAt { get; init; }
    public DateTime? IncidentDateTime { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public Guid? AssignedAuthorityId { get; init; }
    public string? AssignedAuthorityName { get; init; }
    public int? WitnessCount { get; init; }
    public decimal? EstimatedLoss { get; init; }
}

public record IncidentListDto
{
    public Guid IncidentId { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryIcon { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public IncidentStatus Status { get; init; }
    public SeverityLevel Severity { get; init; }
    public string Address { get; init; } = string.Empty;
    public DateTime ReportedAt { get; init; }
}

public record MapIncidentDto
{
    public Guid IncidentId { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public double Lat { get; init; }
    public double Lng { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryIcon { get; init; } = string.Empty;
    public string CategoryColor { get; init; } = string.Empty;
    public IncidentStatus Status { get; init; }
    public SeverityLevel Severity { get; init; }
    public DateTime ReportedAt { get; init; }
}

public record HeatmapPointDto
{
    public double Lat { get; init; }
    public double Lng { get; init; }
    public double Intensity { get; init; }
    public SeverityLevel Severity { get; init; }
}

public record CategoryDto
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"; dotnet build`

Expected: Build succeeded (0 errors)

- [ ] **Step 3: Commit**

```bash
git add SafeZone.Server/DTOs/IncidentDtos.cs
git commit -m "feat: add incident DTOs for reporting and map"
```

---

## Task 2: GeoHelper Utility

**Files:**
- Create: `SafeZone.Server/Helpers/GeoHelper.cs`

- [ ] **Step 1: Write GeoHelper**

```csharp
namespace SafeZone.Server.Helpers;

public static class GeoHelper
{
    private const double EarthRadiusKm = 6371.0;

    public static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }

    public static bool IsValidCoordinate(double lat, double lng)
    {
        return lat >= -90.0 && lat <= 90.0 && lng >= -180.0 && lng <= 180.0;
    }

    public static (double MinLat, double MaxLat, double MinLng, double MaxLng) GetBoundsFromCenter(
        double centerLat, double centerLng, double radiusKm)
    {
        var latChange = radiusKm / 110.574;
        var lngChange = radiusKm / (111.320 * Math.Cos(DegreesToRadians(centerLat)));

        return (
            MinLat: centerLat - latChange,
            MaxLat: centerLat + latChange,
            MinLng: centerLng - lngChange,
            MaxLng: centerLng + lngChange
        );
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`

Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add SafeZone.Server/Helpers/GeoHelper.cs
git commit -m "feat: add GeoHelper for distance calculations"
```

---

## Task 3: Incident Service Interface + Implementation

**Files:**
- Create: `SafeZone.Server/Services/IIncidentService.cs`
- Create: `SafeZone.Server/Services/IncidentService.cs`
- Modify: `SafeZone.Server/Program.cs` (register service)

- [ ] **Step 1: Write IIncidentService.cs**

```csharp
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public interface IIncidentService
{
    Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentDto dto, Guid? reporterId);
    Task<IncidentResponseDto?> GetIncidentByIdAsync(Guid incidentId);
    Task<List<IncidentListDto>> GetMyIncidentsAsync(Guid reporterId);
    Task<List<IncidentListDto>> GetAllIncidentsAsync(
        IncidentStatus? status = null,
        SeverityLevel? severity = null,
        Guid? categoryId = null);
    Task<IncidentResponseDto?> UpdateIncidentAsync(Guid incidentId, UpdateIncidentDto dto, Guid? updatedBy);
    Task<IncidentResponseDto?> UpdateStatusAsync(Guid incidentId, IncidentStatus status, Guid? updatedBy);
    Task<IncidentResponseDto?> AssignAuthorityAsync(Guid incidentId, Guid authorityId);
    Task<List<MapIncidentDto>> GetIncidentsForMapAsync(
        double? minLat = null, double? maxLat = null,
        double? minLng = null, double? maxLng = null,
        IncidentStatus? status = null);
    Task<List<HeatmapPointDto>> GetHeatmapDataAsync(DateTime? since = null);
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<int> GetIncidentCountByStatusAsync(IncidentStatus status);
    Task<Dictionary<SeverityLevel, int>> GetIncidentCountBySeverityAsync();
}
```

- [ ] **Step 2: Write IncidentService.cs**

```csharp
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
                Status = i.Status,
                Severity = i.Severity,
                Address = i.Address,
                ReportedAt = i.ReportedAt
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
                Status = i.Status,
                Severity = i.Severity,
                Address = i.Address,
                ReportedAt = i.ReportedAt
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
```

- [ ] **Step 3: Register service in Program.cs**

Read `Program.cs` and add the service registration. Find where other services are registered (like `builder.Services.AddScoped<IAuthService, AuthService>();`) and add:

```csharp
builder.Services.AddScoped<IIncidentService, IncidentService>();
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`

Expected: Build succeeded (0 errors)

- [ ] **Step 5: Commit**

```bash
git add SafeZone.Server/Services/IIncidentService.cs
git add SafeZone.Server/Services/IncidentService.cs
git add SafeZone.Server/Program.cs
git commit -m "feat: add IIncidentService + IncidentService implementation"
```

---

## Task 4: IncidentController API

**Files:**
- Create: `SafeZone.Server/Controllers/IncidentController.cs`

- [ ] **Step 1: Write IncidentController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using SafeZone.Server.DTOs;
using SafeZone.Server.Hubs;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly IHubContext<MapHub> _mapHub;

    public IncidentController(
        IIncidentService incidentService,
        IHubContext<MapHub> mapHub)
    {
        _incidentService = incidentService;
        _mapHub = mapHub;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _incidentService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<IncidentResponseDto>> CreateIncident(CreateIncidentDto dto)
    {
        var userId = GetCurrentUserId();
        try
        {
            var result = await _incidentService.CreateIncidentAsync(dto, userId);

            await _mapHub.Clients.All.SendAsync("NewIncidentReported", new
            {
                result.IncidentId,
                result.IncidentNumber,
                Lat = result.Latitude,
                Lng = result.Longitude,
                result.Title,
                CategoryName = result.CategoryName,
                result.Severity,
                result.Status,
                Timestamp = DateTime.UtcNow
            });

            return CreatedAtAction(nameof(GetIncident), new { id = result.IncidentId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IncidentResponseDto>> GetIncident(Guid id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);
        if (incident == null)
            return NotFound(new { message = "Incident not found" });

        return Ok(incident);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<IncidentListDto>>> GetMyIncidents()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var incidents = await _incidentService.GetMyIncidentsAsync(userId.Value);
        return Ok(incidents);
    }

    [HttpGet]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<List<IncidentListDto>>> GetAllIncidents(
        [FromQuery] int? status,
        [FromQuery] int? severity,
        [FromQuery] Guid? categoryId)
    {
        var incidents = await _incidentService.GetAllIncidentsAsync(
            status.HasValue ? (Models.IncidentStatus)status.Value : null,
            severity.HasValue ? (Models.SeverityLevel)severity.Value : null,
            categoryId);

        return Ok(incidents);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<IncidentResponseDto>> UpdateIncident(Guid id, UpdateIncidentDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _incidentService.UpdateIncidentAsync(id, dto, userId);

        if (result == null)
            return NotFound(new { message = "Incident not found" });

        if (dto.Status.HasValue)
        {
            await _mapHub.Clients.All.SendAsync("IncidentUpdated", new
            {
                result.IncidentId,
                Status = dto.Status.Value.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<IncidentResponseDto>> UpdateStatus(Guid id, [FromQuery] int status)
    {
        var userId = GetCurrentUserId();
        var incidentStatus = (Models.IncidentStatus)status;

        var result = await _incidentService.UpdateStatusAsync(id, incidentStatus, userId);
        if (result == null)
            return NotFound(new { message = "Incident not found" });

        await _mapHub.Clients.All.SendAsync("IncidentUpdated", new
        {
            result.IncidentId,
            Status = incidentStatus.ToString(),
            Timestamp = DateTime.UtcNow
        });

        if (incidentStatus == Models.IncidentStatus.Resolved || 
            incidentStatus == Models.IncidentStatus.Closed)
        {
            await _mapHub.Clients.All.SendAsync("IncidentResolved", new
            {
                result.IncidentId,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(result);
    }

    [HttpGet("stats/counts")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<object>> GetStats()
    {
        var pending = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.Pending);
        var assigned = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.Assigned);
        var inProgress = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.InProgress);
        var resolved = await _incidentService.GetIncidentCountByStatusAsync(Models.IncidentStatus.Resolved);
        var bySeverity = await _incidentService.GetIncidentCountBySeverityAsync();

        return Ok(new
        {
            statusCounts = new
            {
                pending,
                assigned,
                inProgress,
                resolved
            },
            severityCounts = bySeverity
        });
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`

Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add SafeZone.Server/Controllers/IncidentController.cs
git commit -m "feat: add IncidentController API with real-time broadcasts"
```

---

## Task 5: MapController API

**Files:**
- Create: `SafeZone.Server/Controllers/MapController.cs`

- [ ] **Step 1: Write MapController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeZone.Server.DTOs;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MapController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public MapController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpGet("incidents")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MapIncidentDto>>> GetMapIncidents(
        [FromQuery] double? minLat,
        [FromQuery] double? maxLat,
        [FromQuery] double? minLng,
        [FromQuery] double? maxLng,
        [FromQuery] int? status)
    {
        var incidents = await _incidentService.GetIncidentsForMapAsync(
            minLat, maxLat, minLng, maxLng,
            status.HasValue ? (Models.IncidentStatus)status.Value : null);

        return Ok(incidents);
    }

    [HttpGet("heatmap")]
    [AllowAnonymous]
    public async Task<ActionResult<List<HeatmapPointDto>>> GetHeatmapData(
        [FromQuery] int? daysBack)
    {
        DateTime? since = null;
        if (daysBack.HasValue)
        {
            since = DateTime.UtcNow.AddDays(-daysBack.Value);
        }

        var data = await _incidentService.GetHeatmapDataAsync(since);
        return Ok(data);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _incidentService.GetCategoriesAsync();
        return Ok(categories);
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`

Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add SafeZone.Server/Controllers/MapController.cs
git commit -m "feat: add MapController for incident map and heatmap endpoints"
```

---

## Task 6: Update Client API Wrapper (api.js)

**Files:**
- Modify: `SafeZone.Server/wwwroot/js/api.js`

- [ ] **Step 1: Read current api.js and add incident/map methods**

Add these methods to the `safezoneApi` object:

```javascript
// Incidents
safezoneApi.getCategories = async function() {
    return await this.get('/api/incident/categories');
};

safezoneApi.createIncident = async function(data) {
    return await this.post('/api/incident', data);
};

safezoneApi.getIncident = async function(id) {
    return await this.get(`/api/incident/${id}`);
};

safezoneApi.getMyIncidents = async function() {
    return await this.get('/api/incident/my');
};

safezoneApi.updateIncident = async function(id, data) {
    return await this.put(`/api/incident/${id}`, data);
};

// Map
safezoneApi.getMapIncidents = async function(params) {
    const query = new URLSearchParams();
    if (params.minLat != null) query.append('minLat', params.minLat);
    if (params.maxLat != null) query.append('maxLat', params.maxLat);
    if (params.minLng != null) query.append('minLng', params.minLng);
    if (params.maxLng != null) query.append('maxLng', params.maxLng);
    if (params.status != null) query.append('status', params.status);
    const url = query.toString() ? `/api/map/incidents?${query.toString()}` : '/api/map/incidents';
    return await this.get(url);
};

safezoneApi.getHeatmapData = async function(daysBack) {
    const url = daysBack != null ? `/api/map/heatmap?daysBack=${daysBack}` : '/api/map/heatmap';
    return await this.get(url);
};

safezoneApi.getMapCategories = async function() {
    return await this.get('/api/map/categories');
};
```

- [ ] **Step 2: Commit**

```bash
git add SafeZone.Server/wwwroot/js/api.js
git commit -m "feat: add incident and map API methods to api.js"
```

---

## Task 7: Map JavaScript Module (Leaflet.js)

**Files:**
- Create: `SafeZone.Server/wwwroot/js/map.js`
- Create: `SafeZone.Server/wwwroot/js/geolocation.js`

- [ ] **Step 1: Write js/geolocation.js**

```javascript
(function(window) {
    'use strict';

    const geolocation = {
        getCurrentPosition: async function() {
            return new Promise((resolve, reject) => {
                if (!navigator.geolocation) {
                    reject(new Error('Geolocation not supported'));
                    return;
                }

                navigator.geolocation.getCurrentPosition(
                    (pos) => resolve({
                        lat: pos.coords.latitude,
                        lng: pos.coords.longitude,
                        accuracy: pos.coords.accuracy
                    }),
                    (err) => reject(err),
                    { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 }
                );
            });
        },

        watchPosition: function(onUpdate, onError) {
            if (!navigator.geolocation) {
                if (onError) onError(new Error('Geolocation not supported'));
                return null;
            }

            return navigator.geolocation.watchPosition(
                (pos) => onUpdate({
                    lat: pos.coords.latitude,
                    lng: pos.coords.longitude,
                    accuracy: pos.coords.accuracy
                }),
                (err) => onError && onError(err),
                { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
            );
        },

        clearWatch: function(watchId) {
            if (navigator.geolocation && watchId) {
                navigator.geolocation.clearWatch(watchId);
            }
        },

        getDefaultIslamabad: function() {
            return { lat: 33.6844, lng: 73.0479 };
        }
    };

    window.safezoneGeolocation = geolocation;

})(window);
```

- [ ] **Step 2: Write js/map.js**

```javascript
(function(window) {
    'use strict';

    const maps = {};
    const markers = {};
    const heatmapLayers = {};

    const DEFAULT_CENTER = { lat: 33.6844, lng: 73.0479 };
    const DEFAULT_ZOOM = 13;

    const TILE_URL = 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png';
    const TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>';

    function initMap(containerId, options = {}) {
        if (maps[containerId]) {
            return maps[containerId];
        }

        const center = options.center || DEFAULT_CENTER;
        const zoom = options.zoom || DEFAULT_ZOOM;

        const map = L.map(containerId, {
            zoomControl: options.zoomControl !== false,
            scrollWheelZoom: options.scrollWheelZoom !== false,
            zoomAnimation: true
        }).setView([center.lat, center.lng], zoom);

        L.tileLayer(TILE_URL, {
            attribution: TILE_ATTRIBUTION,
            maxZoom: 19
        }).addTo(map);

        maps[containerId] = map;
        markers[containerId] = [];

        return map;
    }

    function getMap(containerId) {
        return maps[containerId];
    }

    function createMarker(lat, lng, options = {}) {
        const color = options.color || '#00FF88';
        const icon = options.icon || '📍';

        const htmlIcon = L.divIcon({
            html: `<div style="
                width: 36px; height: 36px; 
                background: ${color}20;
                border: 2px solid ${color};
                border-radius: 50%;
                display: flex; align-items: center; justify-content: center;
                font-size: 18px;
                box-shadow: 0 0 12px ${color}60;
            ">${icon}</div>`,
            className: 'custom-marker',
            iconSize: [36, 36],
            iconAnchor: [18, 18]
        });

        const marker = L.marker([lat, lng], { icon: htmlIcon });
        
        if (options.popup) {
            marker.bindPopup(options.popup, {
                maxWidth: 300,
                className: 'map-popup'
            });
        }

        if (options.data) {
            marker.incidentData = options.data;
        }

        return marker;
    }

    function addMarker(containerId, lat, lng, options = {}) {
        const map = maps[containerId];
        if (!map) return null;

        const marker = createMarker(lat, lng, options);
        marker.addTo(map);

        if (!markers[containerId]) {
            markers[containerId] = [];
        }
        markers[containerId].push(marker);

        return marker;
    }

    function clearMarkers(containerId) {
        if (!markers[containerId]) return;
        
        const map = maps[containerId];
        if (!map) return;

        markers[containerId].forEach(m => map.removeLayer(m));
        markers[containerId] = [];
    }

    function addIncidentMarkers(containerId, incidents) {
        clearMarkers(containerId);

        incidents.forEach(incident => {
            const severityColor = getSeverityColor(incident.severity);
            const statusIcon = getStatusIcon(incident.status);
            
            const popupContent = createIncidentPopup(incident);

            addMarker(containerId, incident.lat, incident.lng, {
                color: severityColor,
                icon: statusIcon,
                popup: popupContent,
                data: incident
            });
        });
    }

    function getSeverityColor(severity) {
        const colors = {
            0: '#00FF88',
            1: '#3B82F6',
            2: '#FF9500',
            3: '#FF3B5C'
        };
        return colors[severity] || '#00FF88';
    }

    function getStatusIcon(status) {
        const icons = {
            0: '⏳',
            1: '📋',
            2: '🔄',
            3: '✅',
            4: '📦'
        };
        return icons[status] || '📍';
    }

    function createIncidentPopup(incident) {
        const statusNames = ['Pending', 'Assigned', 'In Progress', 'Resolved', 'Closed'];
        const severityNames = ['Low', 'Medium', 'High', 'Critical'];
        
        return `
            <div style="min-width: 200px;">
                <h4 style="margin: 0 0 8px 0; color: var(--primary);">${incident.title || 'Incident'}</h4>
                <p style="margin: 0 0 6px 0; font-size: 12px; color: var(--text-2);">
                    ${incident.categoryName || 'Incident'}
                </p>
                <div style="display: flex; gap: 8px; margin-bottom: 8px;">
                    <span class="badge badge-${getSeverityBadgeClass(incident.severity)}" style="font-size: 10px;">
                        ${severityNames[incident.severity] || 'Medium'}
                    </span>
                    <span class="badge badge-info" style="font-size: 10px;">
                        ${statusNames[incident.status] || 'Unknown'}
                    </span>
                </div>
                <p style="margin: 0; font-size: 11px; color: var(--text-3);">
                    ${new Date(incident.reportedAt).toLocaleString()}
                </p>
            </div>
        `;
    }

    function getSeverityBadgeClass(severity) {
        const classes = {
            0: 'severity-low',
            1: 'severity-medium',
            2: 'severity-high',
            3: 'severity-critical'
        };
        return classes[severity] || 'info';
    }

    function panTo(containerId, lat, lng, zoom) {
        const map = maps[containerId];
        if (!map) return;

        if (zoom != null) {
            map.setView([lat, lng], zoom, { animate: true });
        } else {
            map.panTo([lat, lng], { animate: true });
        }
    }

    function fitBounds(containerId, incidents) {
        const map = maps[containerId];
        if (!map || !incidents || incidents.length === 0) return;

        const latLngs = incidents.map(i => [i.lat, i.lng]);
        const bounds = L.latLngBounds(latLngs);
        map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
    }

    function enableLocationPicker(containerId, onLocationSelected) {
        const map = maps[containerId];
        if (!map) return;

        let pickerMarker = null;

        map.on('click', function(e) {
            const lat = e.latlng.lat;
            const lng = e.latlng.lng;

            if (pickerMarker) {
                map.removeLayer(pickerMarker);
            }

            pickerMarker = L.marker([lat, lng], {
                icon: L.divIcon({
                    html: `<div style="
                        width: 48px; height: 48px;
                        background: rgba(0, 255, 136, 0.15);
                        border: 3px solid #00FF88;
                        border-radius: 50%;
                        display: flex; align-items: center; justify-content: center;
                        font-size: 20px;
                        box-shadow: 0 0 20px rgba(0, 255, 136, 0.4);
                    ">📍</div>`,
                    className: 'picker-marker',
                    iconSize: [48, 48],
                    iconAnchor: [24, 24]
                })
            }).addTo(map);

            if (onLocationSelected) {
                onLocationSelected({ lat, lng });
            }
        });

        return () => {
            map.off('click');
            if (pickerMarker) {
                map.removeLayer(pickerMarker);
            }
        };
    }

    function addHeatmap(containerId, points) {
        const map = maps[containerId];
        if (!map || !window.L || !L.heatLayer) return null;

        if (heatmapLayers[containerId]) {
            map.removeLayer(heatmapLayers[containerId]);
        }

        const heatData = points.map(p => [p.lat, p.lng, p.intensity]);

        const heatLayer = L.heatLayer(heatData, {
            radius: 25,
            blur: 15,
            maxZoom: 15,
            gradient: {
                0.4: '#00FF88',
                0.6: '#FF9500',
                0.8: '#FF3B5C'
            }
        }).addTo(map);

        heatmapLayers[containerId] = heatLayer;
        return heatLayer;
    }

    function removeHeatmap(containerId) {
        const map = maps[containerId];
        if (!map || !heatmapLayers[containerId]) return;

        map.removeLayer(heatmapLayers[containerId]);
        delete heatmapLayers[containerId];
    }

    const safezoneMap = {
        init: initMap,
        get: getMap,
        addMarker,
        clearMarkers,
        addIncidentMarkers,
        panTo,
        fitBounds,
        enableLocationPicker,
        addHeatmap,
        removeHeatmap,
        getSeverityColor,
        DEFAULT_CENTER,
        DEFAULT_ZOOM
    };

    window.safezoneMap = safezoneMap;

})(window);
```

- [ ] **Step 3: Commit**

```bash
git add SafeZone.Server/wwwroot/js/map.js
git add SafeZone.Server/wwwroot/js/geolocation.js
git commit -m "feat: add map.js (Leaflet wrapper) and geolocation.js"
```

---

## Task 8: User Dashboard - Integrate Leaflet Map

**Files:**
- Modify: `SafeZone.Server/wwwroot/user/dashboard.html`

- [ ] **Step 1: Add Leaflet CSS/JS CDNs**

In the `<head>` section, add:

```html
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script src="https://unpkg.com/leaflet.heat@0.2.0/dist/leaflet-heat.js"></script>
```

- [ ] **Step 2: Include new JS files**

Before the closing `</body>`, add:

```html
<script src="../js/geolocation.js"></script>
<script src="../js/map.js"></script>
```

- [ ] **Step 3: Replace "Coming Soon" map placeholder with actual map**

Replace the map section div with:

```html
<div class="md:col-span-2 glass-elevated p-0 overflow-hidden" style="min-height: 400px; border-radius: 16px;">
    <div id="user-map" style="width: 100%; height: 400px; position: relative;"></div>
    <div class="p-4 flex items-center justify-between" style="background: var(--bg-surface); border-top: 1px solid var(--glass-border);">
        <div class="flex items-center gap-3">
            <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" id="heatmapToggle" class="w-4 h-4">
                <span class="text-sm">Heatmap</span>
            </label>
            <div class="h-4 w-px" style="background: var(--glass-border);"></div>
            <span class="text-sm text-muted" id="mapIncidentCount">Loading incidents...</span>
        </div>
        <button id="refreshMapBtn" class="btn btn-secondary btn-sm">
            ↻ Refresh
        </button>
    </div>
</div>
```

- [ ] **Step 4: Add map initialization script**

Add a script section at the bottom:

```html
<script>
(async function initDashboardMap() {
    const api = window.safezoneApi;
    const mapModule = window.safezoneMap;
    const toast = window.safezoneToast;

    let map;
    let showHeatmap = false;
    let currentIncidents = [];

    try {
        let center = mapModule.DEFAULT_CENTER;
        
        try {
            const pos = await window.safezoneGeolocation.getCurrentPosition();
            center = { lat: pos.lat, lng: pos.lng };
        } catch (e) {
            console.log('Using default location');
        }

        map = mapModule.init('user-map', { center, zoom: 14 });
        loadIncidents();

        document.getElementById('refreshMapBtn').addEventListener('click', loadIncidents);
        document.getElementById('heatmapToggle').addEventListener('change', toggleHeatmap);

    } catch (e) {
        console.error('Map init failed:', e);
        toast.error('Failed to load map');
    }

    async function loadIncidents() {
        try {
            currentIncidents = await api.getMapIncidents({});
            
            if (currentIncidents && currentIncidents.length > 0) {
                mapModule.addIncidentMarkers('user-map', currentIncidents);
                
                if (currentIncidents.length > 1) {
                    mapModule.fitBounds('user-map', currentIncidents);
                }
                
                document.getElementById('mapIncidentCount').textContent = 
                    `${currentIncidents.length} active incident${currentIncidents.length !== 1 ? 's' : ''}`;
            } else {
                document.getElementById('mapIncidentCount').textContent = 'No active incidents';
            }

            if (showHeatmap) {
                updateHeatmap();
            }
        } catch (e) {
            console.error('Failed to load incidents:', e);
            document.getElementById('mapIncidentCount').textContent = 'Failed to load';
        }
    }

    function toggleHeatmap(e) {
        showHeatmap = e.target.checked;
        if (showHeatmap) {
            updateHeatmap();
        } else {
            mapModule.removeHeatmap('user-map');
        }
    }

    async function updateHeatmap() {
        try {
            const heatmapData = await api.getHeatmapData(7);
            if (heatmapData && heatmapData.length > 0) {
                mapModule.addHeatmap('user-map', heatmapData);
            }
        } catch (e) {
            console.error('Heatmap failed:', e);
        }
    }
})();
</script>
```

- [ ] **Step 5: Build and test**

Run: `dotnet build`

- [ ] **Step 6: Commit**

```bash
git add SafeZone.Server/wwwroot/user/dashboard.html
git commit -m "feat: integrate Leaflet map into user dashboard"
```

---

## Task 9: 4-Step Report Incident Form

**Files:**
- Replace: `SafeZone.Server/wwwroot/user/report-incident.html`

- [ ] **Step 1: Write complete report-incident.html**

Full file content - 4-step wizard with map location picker:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Report Incident - SafeZone</title>
    
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&family=Inter:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;600&display=swap" rel="stylesheet">
    
    <script src="https://cdn.tailwindcss.com"></script>
    
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    
    <link rel="stylesheet" href="../css/global.css">
</head>
<body class="min-h-screen page-enter">
    <div class="ambient-glow-2"></div>

    <nav class="glass fixed top-0 left-0 right-0 z-50" style="border-radius: 0;">
        <div class="max-w-4xl mx-auto px-4">
            <div class="flex items-center justify-between h-16">
                <div class="flex items-center gap-3">
                    <a href="dashboard.html" class="flex items-center gap-2 text-muted hover:text-white transition-colors">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path>
                        </svg>
                        <span class="text-sm">Back</span>
                    </a>
                </div>
                <h1 class="font-display text-lg font-bold" style="color: var(--primary);">Report Incident</h1>
                <div class="w-16"></div>
            </div>
        </div>
    </nav>

    <div class="pt-20 pb-8 px-4 max-w-4xl mx-auto">
        <div class="flex items-center justify-center gap-2 mb-8">
            <div class="step-indicator" data-step="1">
                <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold" 
                     id="step1Dot" style="background: var(--primary); color: var(--bg-void);">1</div>
            </div>
            <div class="w-16 h-0.5" id="line1" style="background: var(--glass-border);"></div>
            <div class="step-indicator" data-step="2">
                <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold" 
                     id="step2Dot" style="background: var(--glass); color: var(--text-3); border: 1px solid var(--glass-border);">2</div>
            </div>
            <div class="w-16 h-0.5" id="line2" style="background: var(--glass-border);"></div>
            <div class="step-indicator" data-step="3">
                <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold" 
                     id="step3Dot" style="background: var(--glass); color: var(--text-3); border: 1px solid var(--glass-border);">3</div>
            </div>
            <div class="w-16 h-0.5" id="line3" style="background: var(--glass-border);"></div>
            <div class="step-indicator" data-step="4">
                <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold" 
                     id="step4Dot" style="background: var(--glass); color: var(--text-3); border: 1px solid var(--glass-border);">4</div>
            </div>
        </div>

        <div id="step1" class="step-content">
            <div class="glass-elevated p-8">
                <h2 class="font-display text-2xl font-bold mb-2">Step 1: Incident Type</h2>
                <p class="text-muted mb-6">Select the category that best describes what happened</p>
                
                <div class="grid grid-cols-2 md:grid-cols-3 gap-4" id="categoryGrid">
                </div>

                <div class="mt-8 pt-6" style="border-top: 1px solid var(--glass-border);">
                    <label class="block text-sm font-medium mb-3">Severity Level</label>
                    <div class="grid grid-cols-4 gap-3">
                        <button type="button" class="severity-btn p-4 rounded-lg text-center transition-all" 
                                data-severity="0" style="background: var(--glass); border: 2px solid transparent;">
                            <div class="text-2xl mb-1">🟢</div>
                            <div class="text-sm font-medium">Low</div>
                        </button>
                        <button type="button" class="severity-btn p-4 rounded-lg text-center transition-all" 
                                data-severity="1" style="background: var(--glass); border: 2px solid transparent;">
                            <div class="text-2xl mb-1">🔵</div>
                            <div class="text-sm font-medium">Medium</div>
                        </button>
                        <button type="button" class="severity-btn p-4 rounded-lg text-center transition-all selected" 
                                data-severity="2" style="background: var(--warning-dim); border: 2px solid var(--warning);">
                            <div class="text-2xl mb-1">🟠</div>
                            <div class="text-sm font-medium">High</div>
                        </button>
                        <button type="button" class="severity-btn p-4 rounded-lg text-center transition-all" 
                                data-severity="3" style="background: var(--glass); border: 2px solid transparent;">
                            <div class="text-2xl mb-1">🔴</div>
                            <div class="text-sm font-medium">Critical</div>
                        </button>
                    </div>
                </div>

                <div class="flex justify-end mt-8">
                    <button type="button" id="toStep2" class="btn btn-primary" disabled>
                        Continue →
                    </button>
                </div>
            </div>
        </div>

        <div id="step2" class="step-content" style="display: none;">
            <div class="glass-elevated p-8">
                <h2 class="font-display text-2xl font-bold mb-2">Step 2: Location</h2>
                <p class="text-muted mb-6">Click on the map to mark where the incident occurred</p>
                
                <div class="flex gap-3 mb-4">
                    <button type="button" id="useMyLocation" class="btn btn-secondary btn-sm">
                        📍 Use My Location
                    </button>
                    <button type="button" id="useIslamabad" class="btn btn-secondary btn-sm">
                        🏛️ Use Default (Islamabad)
                    </button>
                </div>

                <div id="report-map" style="width: 100%; height: 350px; border-radius: 12px; overflow-hidden; border: 1px solid var(--glass-border);"></div>
                
                <div class="mt-4">
                    <label class="block text-sm font-medium mb-2">Address (Optional)</label>
                    <input type="text" id="incidentAddress" class="form-input w-full" 
                           placeholder="e.g., F-7 Markaz, Islamabad">
                </div>

                <div class="mt-4 p-4 rounded-lg" id="locationStatus" style="background: var(--glass);">
                    <p class="text-sm text-muted text-center">No location selected</p>
                </div>

                <div class="flex justify-between mt-8">
                    <button type="button" id="backToStep1" class="btn btn-secondary">
                        ← Back
                    </button>
                    <button type="button" id="toStep3" class="btn btn-primary" disabled>
                        Continue →
                    </button>
                </div>
            </div>
        </div>

        <div id="step3" class="step-content" style="display: none;">
            <div class="glass-elevated p-8">
                <h2 class="font-display text-2xl font-bold mb-2">Step 3: Details</h2>
                <p class="text-muted mb-6">Provide more information about the incident</p>
                
                <div class="space-y-5">
                    <div>
                        <label class="block text-sm font-medium mb-2">Title *</label>
                        <input type="text" id="incidentTitle" class="form-input w-full" 
                               placeholder="Short summary of what happened" maxlength="100">
                        <p class="text-xs text-muted mt-1" id="titleCount">0/100</p>
                    </div>

                    <div>
                        <label class="block text-sm font-medium mb-2">Description *</label>
                        <textarea id="incidentDescription" class="form-input w-full" rows="5" 
                                  placeholder="Describe what happened in detail..."></textarea>
                    </div>

                    <div>
                        <label class="block text-sm font-medium mb-2">When did it happen?</label>
                        <input type="datetime-local" id="incidentDateTime" class="form-input w-full">
                    </div>

                    <div class="grid grid-cols-2 gap-4">
                        <div>
                            <label class="block text-sm font-medium mb-2">Witness Count</label>
                            <input type="number" id="witnessCount" class="form-input w-full" min="0" placeholder="0">
                        </div>
                        <div>
                            <label class="block text-sm font-medium mb-2">Estimated Loss (PKR)</label>
                            <input type="number" id="estimatedLoss" class="form-input w-full" min="0" placeholder="0">
                        </div>
                    </div>

                    <div>
                        <label class="block text-sm font-medium mb-2">Suspect Description (if applicable)</label>
                        <textarea id="suspectDescription" class="form-input w-full" rows="3" 
                                  placeholder="Describe any suspects..."></textarea>
                    </div>

                    <div class="flex items-center gap-3 p-4 rounded-lg" style="background: var(--glass);">
                        <input type="checkbox" id="isAnonymous" class="w-5 h-5">
                        <div>
                            <label for="isAnonymous" class="font-medium cursor-pointer">Submit Anonymously</label>
                            <p class="text-xs text-muted">Your identity will not be shown</p>
                        </div>
                    </div>
                </div>

                <div class="flex justify-between mt-8">
                    <button type="button" id="backToStep2" class="btn btn-secondary">
                        ← Back
                    </button>
                    <button type="button" id="toStep4" class="btn btn-primary" disabled>
                        Continue →
                    </button>
                </div>
            </div>
        </div>

        <div id="step4" class="step-content" style="display: none;">
            <div class="glass-elevated p-8">
                <h2 class="font-display text-2xl font-bold mb-2">Step 4: Review & Submit</h2>
                <p class="text-muted mb-6">Review your report before submitting</p>
                
                <div class="space-y-4">
                    <div class="p-4 rounded-lg" style="background: var(--glass);">
                        <h4 class="text-sm text-muted uppercase tracking-wider mb-2">Incident Type</h4>
                        <p class="font-semibold" id="reviewCategory">-</p>
                        <p class="text-sm mt-1">
                            Severity: <span id="reviewSeverity" class="badge">-</span>
                        </p>
                    </div>

                    <div class="p-4 rounded-lg" style="background: var(--glass);">
                        <h4 class="text-sm text-muted uppercase tracking-wider mb-2">Location</h4>
                        <p class="font-semibold" id="reviewLocation">-</p>
                        <p class="text-sm text-muted mt-1" id="reviewAddress">-</p>
                    </div>

                    <div class="p-4 rounded-lg" style="background: var(--glass);">
                        <h4 class="text-sm text-muted uppercase tracking-wider mb-2">Details</h4>
                        <p class="font-semibold" id="reviewTitle">-</p>
                        <p class="text-sm text-muted mt-2 whitespace-pre-wrap" id="reviewDescription">-</p>
                    </div>

                    <div class="p-4 rounded-lg" style="background: var(--glass);">
                        <h4 class="text-sm text-muted uppercase tracking-wider mb-2">Reporting As</h4>
                        <p class="font-semibold" id="reportingAs">-</p>
                    </div>
                </div>

                <div class="flex justify-between mt-8">
                    <button type="button" id="backToStep3" class="btn btn-secondary">
                        ← Back
                    </button>
                    <button type="button" id="submitReport" class="btn btn-primary btn-lg">
                        ✓ Submit Report
                    </button>
                </div>
            </div>
        </div>

        <div id="successScreen" style="display: none;">
            <div class="glass-elevated p-12 text-center">
                <div class="w-20 h-20 rounded-full mx-auto mb-6 flex items-center justify-center" 
                     style="background: var(--primary-dim);">
                    <span class="text-4xl">✅</span>
                </div>
                <h2 class="font-display text-2xl font-bold mb-2">Report Submitted!</h2>
                <p class="text-muted mb-4">Your incident report has been received</p>
                
                <div class="p-4 rounded-lg inline-block mb-6" style="background: var(--glass);">
                    <p class="text-sm text-muted mb-1">Incident Number</p>
                    <p class="font-display text-xl font-bold" id="newIncidentNumber" style="color: var(--primary);">-</p>
                </div>

                <div class="flex flex-col sm:flex-row gap-3 justify-center">
                    <a href="my-incidents.html" class="btn btn-primary">
                        View My Reports
                    </a>
                    <a href="dashboard.html" class="btn btn-secondary">
                        Back to Dashboard
                    </a>
                </div>
            </div>
        </div>
    </div>

    <script src="../js/api.js"></script>
    <script src="../js/auth.js"></script>
    <script src="../js/toast.js"></script>
    <script src="../js/geolocation.js"></script>
    <script src="../js/map.js"></script>

    <script>
(function() {
    const api = window.safezoneApi;
    const toast = window.safezoneToast;
    const mapModule = window.safezoneMap;

    if (!window.safezoneAuth.requireAuth()) return;

    const formData = {
        categoryId: null,
        categoryName: '',
        severity: 2,
        lat: null,
        lng: null,
        address: '',
        title: '',
        description: '',
        incidentDateTime: null,
        witnessCount: null,
        estimatedLoss: null,
        suspectDescription: '',
        isAnonymous: false
    };

    let currentStep = 1;
    let categories = [];
    let map;
    let locationCleanup = null;

    init();

    async function init() {
        categories = await api.getCategories();
        renderCategories();
        setupEventListeners();
        updateStepIndicators();

        const now = new Date();
        now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
        document.getElementById('incidentDateTime').value = now.toISOString().slice(0, 16);
    }

    function renderCategories() {
        const grid = document.getElementById('categoryGrid');
        grid.innerHTML = categories.map(cat => `
            <button type="button" class="category-btn p-4 rounded-lg text-center transition-all" 
                    data-id="${cat.categoryId}" style="background: var(--glass); border: 2px solid transparent;">
                <div class="text-3xl mb-2">${cat.icon}</div>
                <div class="font-medium text-sm">${cat.name}</div>
                <div class="text-xs text-muted mt-1">${cat.description || ''}</div>
            </button>
        `).join('');
    }

    function setupEventListeners() {
        document.getElementById('categoryGrid').addEventListener('click', function(e) {
            const btn = e.target.closest('.category-btn');
            if (!btn) return;

            document.querySelectorAll('.category-btn').forEach(b => {
                b.style.background = 'var(--glass)';
                b.style.borderColor = 'transparent';
            });

            btn.style.background = 'var(--primary-dim)';
            btn.style.borderColor = 'var(--primary)';

            formData.categoryId = btn.dataset.id;
            formData.categoryName = categories.find(c => c.categoryId === btn.dataset.id)?.name || '';

            document.getElementById('toStep2').disabled = false;
        });

        document.querySelectorAll('.severity-btn').forEach(btn => {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.severity-btn').forEach(b => {
                    b.style.background = 'var(--glass)';
                    b.style.borderColor = 'transparent';
                    b.classList.remove('selected');
                });

                const severity = parseInt(this.dataset.severity);
                const colors = ['var(--primary-dim)', 'var(--info-dim)', 'var(--warning-dim)', 'var(--danger-dim)'];
                const borders = ['var(--primary)', 'var(--info)', 'var(--warning)', 'var(--danger)'];
                
                this.style.background = colors[severity];
                this.style.borderColor = borders[severity];
                this.classList.add('selected');

                formData.severity = severity;
            });
        });

        document.getElementById('toStep2').addEventListener('click', () => goToStep(2));
        document.getElementById('backToStep1').addEventListener('click', () => goToStep(1));
        document.getElementById('toStep3').addEventListener('click', () => goToStep(3));
        document.getElementById('backToStep2').addEventListener('click', () => goToStep(2));
        document.getElementById('toStep4').addEventListener('click', () => goToStep(4));
        document.getElementById('backToStep3').addEventListener('click', () => goToStep(3));

        document.getElementById('useMyLocation').addEventListener('click', async function() {
            try {
                const pos = await window.safezoneGeolocation.getCurrentPosition();
                setMapLocation(pos.lat, pos.lng);
            } catch (e) {
                toast.error('Could not get your location');
            }
        });

        document.getElementById('useIslamabad').addEventListener('click', function() {
            setMapLocation(33.6844, 73.0479);
        });

        document.getElementById('incidentTitle').addEventListener('input', function() {
            document.getElementById('titleCount').textContent = `${this.value.length}/100`;
            validateStep3();
        });

        document.getElementById('incidentDescription').addEventListener('input', validateStep3);
        document.getElementById('isAnonymous').addEventListener('change', function() {
            formData.isAnonymous = this.checked;
        });

        document.getElementById('submitReport').addEventListener('click', submitReport);
    }

    function goToStep(step) {
        document.querySelectorAll('.step-content').forEach(el => el.style.display = 'none');
        document.getElementById(`step${step}`).style.display = 'block';
        currentStep = step;
        updateStepIndicators();

        if (step === 2) {
            initMap();
        } else if (step === 4) {
            updateReview();
        }
    }

    function updateStepIndicators() {
        const steps = [1, 2, 3, 4];
        
        steps.forEach(step => {
            const dot = document.getElementById(`step${step}Dot`);
            const line = step < 4 ? document.getElementById(`line${step}`) : null;

            if (step < currentStep) {
                dot.style.background = 'var(--primary)';
                dot.style.color = 'var(--bg-void)';
                dot.style.border = 'none';
                if (line) line.style.background = 'var(--primary)';
            } else if (step === currentStep) {
                dot.style.background = 'var(--primary)';
                dot.style.color = 'var(--bg-void)';
                dot.style.border = 'none';
            } else {
                dot.style.background = 'var(--glass)';
                dot.style.color = 'var(--text-3)';
                dot.style.border = '1px solid var(--glass-border)';
                if (line) line.style.background = 'var(--glass-border)';
            }
        });
    }

    function initMap() {
        if (map) return;

        let center = mapModule.DEFAULT_CENTER;
        if (formData.lat !== null && formData.lng !== null) {
            center = { lat: formData.lat, lng: formData.lng };
        }

        map = mapModule.init('report-map', { center, zoom: 14 });
        
        locationCleanup = mapModule.enableLocationPicker('report-map', function(pos) {
            setMapLocation(pos.lat, pos.lng);
        });
    }

    function setMapLocation(lat, lng) {
        formData.lat = lat;
        formData.lng = lng;

        mapModule.clearMarkers('report-map');
        mapModule.addMarker('report-map', lat, lng, {
            color: '#00FF88',
            icon: '📍'
        });
        mapModule.panTo('report-map', lat, lng, 15);

        formData.address = document.getElementById('incidentAddress').value;
        
        document.getElementById('locationStatus').innerHTML = `
            <p class="text-sm text-center">
                <span style="color: var(--primary);">✓ Location selected</span><br>
                <span class="text-muted">${lat.toFixed(6)}, ${lng.toFixed(6)}</span>
            </p>
        `;

        document.getElementById('toStep3').disabled = false;
    }

    function validateStep3() {
        const title = document.getElementById('incidentTitle').value.trim();
        const desc = document.getElementById('incidentDescription').value.trim();
        
        document.getElementById('toStep4').disabled = !(title.length > 0 && desc.length > 0);
    }

    function updateReview() {
        const user = window.safezoneAuth.getUser();
        
        formData.title = document.getElementById('incidentTitle').value.trim();
        formData.description = document.getElementById('incidentDescription').value.trim();
        formData.incidentDateTime = document.getElementById('incidentDateTime').value || null;
        formData.witnessCount = parseInt(document.getElementById('witnessCount').value) || null;
        formData.estimatedLoss = parseFloat(document.getElementById('estimatedLoss').value) || null;
        formData.suspectDescription = document.getElementById('suspectDescription').value.trim();

        const severityNames = ['Low', 'Medium', 'High', 'Critical'];
        const severityBadges = ['badge-severity-low', 'badge-severity-medium', 'badge-severity-high', 'badge-severity-critical'];

        document.getElementById('reviewCategory').textContent = formData.categoryName;
        document.getElementById('reviewSeverity').textContent = severityNames[formData.severity];
        document.getElementById('reviewSeverity').className = `badge ${severityBadges[formData.severity]}`;
        document.getElementById('reviewLocation').textContent = `${formData.lat?.toFixed(6)}, ${formData.lng?.toFixed(6)}`;
        document.getElementById('reviewAddress').textContent = formData.address || 'No address provided';
        document.getElementById('reviewTitle').textContent = formData.title;
        document.getElementById('reviewDescription').textContent = formData.description;
        document.getElementById('reportingAs').textContent = formData.isAnonymous ? 'Anonymous' : (user?.fullName || 'Logged in user');
    }

    async function submitReport() {
        const btn = document.getElementById('submitReport');
        btn.disabled = true;
        btn.textContent = 'Submitting...';

        try {
            const result = await api.createIncident({
                categoryId: formData.categoryId,
                latitude: formData.lat,
                longitude: formData.lng,
                address: formData.address,
                title: formData.title,
                description: formData.description,
                severity: formData.severity,
                isAnonymous: formData.isAnonymous,
                incidentDateTime: formData.incidentDateTime ? new Date(formData.incidentDateTime).toISOString() : null,
                witnessCount: formData.witnessCount,
                estimatedLoss: formData.estimatedLoss,
                suspectDescription: formData.suspectDescription || null
            });

            document.querySelectorAll('.step-content').forEach(el => el.style.display = 'none');
            document.getElementById('successScreen').style.display = 'block';
            document.getElementById('newIncidentNumber').textContent = result.incidentNumber;

            toast.success('Incident reported successfully!');

        } catch (e) {
            console.error('Submit failed:', e);
            toast.error('Failed to submit report. Please try again.');
            btn.disabled = false;
            btn.textContent = '✓ Submit Report';
        }
    }
})();
    </script>
</body>
</html>
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build`

Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add SafeZone.Server/wwwroot/user/report-incident.html
git commit -m "feat: complete 4-step report incident form with map location picker"
```

---

## Task 10: My Incidents Page

**Files:**
- Replace: `SafeZone.Server/wwwroot/user/my-incidents.html`

- [ ] **Step 1: Write complete my-incidents.html**

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>My Reports - SafeZone</title>
    
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&family=Inter:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght=400;600&display=swap" rel="stylesheet">
    
    <script src="https://cdn.tailwindcss.com"></script>
    <link rel="stylesheet" href="../css/global.css">
</head>
<body class="min-h-screen page-enter">
    <div class="ambient-glow-2"></div>

    <nav class="glass fixed top-0 left-0 right-0 z-50" style="border-radius: 0;">
        <div class="max-w-4xl mx-auto px-4">
            <div class="flex items-center justify-between h-16">
                <div class="flex items-center gap-3">
                    <a href="dashboard.html" class="flex items-center gap-2 text-muted hover:text-white transition-colors">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path>
                        </svg>
                        <span class="text-sm">Back to Dashboard</span>
                    </a>
                </div>
                <h1 class="font-display text-lg font-bold" style="color: var(--primary);">My Reports</h1>
                <div class="w-16"></div>
            </div>
        </div>
    </nav>

    <div class="pt-20 pb-8 px-4 max-w-4xl mx-auto">
        <div class="flex justify-between items-center mb-6">
            <div>
                <h2 class="font-display text-2xl font-bold">Your Incident Reports</h2>
                <p class="text-muted text-sm mt-1" id="reportCount">Loading...</p>
            </div>
            <a href="report-incident.html" class="btn btn-primary">
                + New Report
            </a>
        </div>

        <div class="flex flex-wrap gap-2 mb-6">
            <button type="button" class="status-filter btn btn-secondary btn-sm" data-status="all" style="background: var(--primary-dim); border-color: var(--primary);">
                All
            </button>
            <button type="button" class="status-filter btn btn-secondary btn-sm" data-status="0">
                ⏳ Pending
            </button>
            <button type="button" class="status-filter btn btn-secondary btn-sm" data-status="1">
                📋 Assigned
            </button>
            <button type="button" class="status-filter btn btn-secondary btn-sm" data-status="2">
                🔄 In Progress
            </button>
            <button type="button" class="status-filter btn btn-secondary btn-sm" data-status="3">
                ✅ Resolved
            </button>
        </div>

        <div id="loadingState" class="glass-elevated p-12 text-center">
            <div class="inline-block w-8 h-8 border-2 border-t-transparent rounded-full animate-spin" 
                 style="border-color: var(--primary); border-top-color: transparent;"></div>
            <p class="text-muted mt-4">Loading your reports...</p>
        </div>

        <div id="emptyState" class="glass-elevated p-12 text-center" style="display: none;">
            <div class="w-20 h-20 rounded-full mx-auto mb-6 flex items-center justify-center" style="background: var(--glass);">
                <span class="text-4xl">📁</span>
            </div>
            <h3 class="font-semibold text-xl mb-2">No Reports Yet</h3>
            <p class="text-muted mb-6">You haven't submitted any incident reports</p>
            <a href="report-incident.html" class="btn btn-primary">
                Submit Your First Report
            </a>
        </div>

        <div id="noMatchState" class="glass-elevated p-12 text-center" style="display: none;">
            <div class="w-20 h-20 rounded-full mx-auto mb-6 flex items-center justify-center" style="background: var(--glass);">
                <span class="text-4xl">🔍</span>
            </div>
            <h3 class="font-semibold text-xl mb-2">No Matching Reports</h3>
            <p class="text-muted">Try selecting a different status filter</p>
        </div>

        <div id="incidentsList" class="space-y-4" style="display: none;">
        </div>
    </div>

    <script src="../js/api.js"></script>
    <script src="../js/auth.js"></script>
    <script src="../js/toast.js"></script>

    <script>
(function() {
    const api = window.safezoneApi;
    const toast = window.safezoneToast;

    if (!window.safezoneAuth.requireAuth()) return;

    let allIncidents = [];
    let currentFilter = 'all';

    const statusNames = ['Pending', 'Assigned', 'In Progress', 'Resolved', 'Closed'];
    const statusColors = ['var(--warning)', 'var(--info)', 'var(--primary)', 'var(--primary)', 'var(--text-3)'];
    const statusIcons = ['⏳', '📋', '🔄', '✅', '📦'];
    const statusBadges = [
        'badge-severity-high',
        'badge-info',
        'badge-primary',
        'badge-severity-low',
        'badge-secondary'
    ];

    const severityNames = ['Low', 'Medium', 'High', 'Critical'];
    const severityBadges = [
        'badge-severity-low',
        'badge-severity-medium',
        'badge-severity-high',
        'badge-severity-critical'
    ];

    init();

    async function init() {
        setupFilters();
        await loadIncidents();
    }

    function setupFilters() {
        document.querySelectorAll('.status-filter').forEach(btn => {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.status-filter').forEach(b => {
                    b.style.background = 'var(--glass)';
                    b.style.borderColor = 'var(--glass-border)';
                });

                this.style.background = 'var(--primary-dim)';
                this.style.borderColor = 'var(--primary)';

                currentFilter = this.dataset.status;
                renderIncidents();
            });
        });
    }

    async function loadIncidents() {
        try {
            allIncidents = await api.getMyIncidents();
            document.getElementById('reportCount').textContent = 
                `${allIncidents.length} report${allIncidents.length !== 1 ? 's' : ''} total`;
            
            renderIncidents();

        } catch (e) {
            console.error('Failed to load:', e);
            document.getElementById('loadingState').style.display = 'none';
            document.getElementById('emptyState').style.display = 'block';
            document.getElementById('reportCount').textContent = 'Failed to load';
        }
    }

    function renderIncidents() {
        document.getElementById('loadingState').style.display = 'none';

        let filtered = allIncidents;
        if (currentFilter !== 'all') {
            filtered = allIncidents.filter(i => i.status === parseInt(currentFilter));
        }

        if (allIncidents.length === 0) {
            document.getElementById('emptyState').style.display = 'block';
            document.getElementById('noMatchState').style.display = 'none';
            document.getElementById('incidentsList').style.display = 'none';
            return;
        }

        if (filtered.length === 0) {
            document.getElementById('emptyState').style.display = 'none';
            document.getElementById('noMatchState').style.display = 'block';
            document.getElementById('incidentsList').style.display = 'none';
            return;
        }

        document.getElementById('emptyState').style.display = 'none';
        document.getElementById('noMatchState').style.display = 'none';
        document.getElementById('incidentsList').style.display = 'block';

        const list = document.getElementById('incidentsList');
        list.innerHTML = filtered.map(incident => createIncidentCard(incident)).join('');
    }

    function createIncidentCard(incident) {
        const reportedDate = new Date(incident.reportedAt);
        const formattedDate = reportedDate.toLocaleDateString('en-US', {
            year: 'numeric', month: 'short', day: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });

        return `
            <div class="incident-card glass-elevated p-6 hover:scale-[1.01] transition-transform cursor-pointer"
                 data-id="${incident.incidentId}">
                <div class="flex items-start justify-between mb-4">
                    <div>
                        <div class="flex items-center gap-3 mb-2">
                            <h3 class="font-semibold text-lg">${escapeHtml(incident.title)}</h3>
                        </div>
                        <p class="font-mono text-sm" style="color: var(--primary);">${escapeHtml(incident.incidentNumber)}</p>
                    </div>
                    <div class="flex flex-col items-end gap-2">
                        <span class="badge ${statusBadges[incident.status]}">
                            ${statusIcons[incident.status]} ${statusNames[incident.status]}
                        </span>
                        <span class="badge ${severityBadges[incident.severity]}">
                            ${severityNames[incident.severity]}
                        </span>
                    </div>
                </div>
                
                <div class="flex flex-wrap gap-4 text-sm text-muted mb-4">
                    <span>📁 ${escapeHtml(incident.categoryName)}</span>
                    ${incident.address ? `<span>📍 ${escapeHtml(incident.address)}</span>` : ''}
                    <span>🕐 ${formattedDate}</span>
                </div>
            </div>
        `;
    }

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
})();
    </script>
</body>
</html>
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`

- [ ] **Step 3: Commit**

```bash
git add SafeZone.Server/wwwroot/user/my-incidents.html
git commit -m "feat: complete my-incidents page with filtering"
```

---

## Task 11: Final Build & Test

**Files:** All (run build)

- [ ] **Step 1: Stop any running server**

- [ ] **Step 2: Full rebuild**

Run: `cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"; dotnet build`

Expected: Build succeeded (0 errors, 0 warnings)

- [ ] **Step 3: Start server for testing**

Run: `dotnet run --urls "http://localhost:5000"`

- [ ] **Step 4: Verify endpoints manually (or via Swagger)**

Visit: `http://localhost:5000/swagger`

Test:
- GET `/api/incident/categories` - Should return seeded categories
- GET `/api/map/incidents` - Should return active incidents

- [ ] **Step 5: Commit final**

```bash
git status
git add -A
git commit -m "feat: complete Phase A - Incident System with Leaflet map integration"
```

---

## Plan Self-Review

### 1. Spec Coverage

| Requirement | Task | Coverage |
|------------|------|----------|
| IncidentController + IncidentService | Tasks 3, 4 | ✅ Complete |
| 4-step Report Incident form | Task 9 | ✅ Complete |
| Leaflet.js map integration | Tasks 7, 8 | ✅ Complete |
| MapController (map incidents, heatmap) | Task 5 | ✅ Complete |
| My Incidents page | Task 10 | ✅ Complete |
| Real-time SignalR broadcasts | Task 4 (IncidentController) | ✅ Complete |
| Incident DTOs | Task 1 | ✅ Complete |
| GeoHelper utility | Task 2 | ✅ Complete |
| Geolocation wrapper | Task 7 | ✅ Complete |

### 2. Placeholder Scan

No "TBD", "TODO", or incomplete sections. All tasks have complete code.

### 3. Type Consistency

- Incident status enum: `Pending=0, Assigned=1, InProgress=2, Resolved=3, Closed=4` — consistent across all files
- Severity enum: `Low=0, Medium=1, High=2, Critical=3` — consistent
- API endpoints match method names in api.js
- Map DTO fields match Leaflet expectations

### 4. Scope Check

This plan focuses specifically on **Phase A: Incident System** as requested. It is self-contained and testable:
- Server-side: All needed services and controllers are built
- Client-side: User-facing pages with map integration are ready
- The system can be demo'd: Login → View Map → Report Incident → View My Incidents

---

## Next Steps After Phase A

Once Phase A is complete and verified, the remaining phases are:

| Phase | Features |
|-------|----------|
| **Phase B: Authority Dashboard** | Incident management, Kanban board, Broadcast alerts, Authority map view |
| **Phase C: FIR System** | FIRController, 4-step FIR wizard, QuestPDF generation, Authority FIR review |
| **Phase D: SOS + AI Calling** | SOS page, Twilio mock mode, GPT-4o script generation mock, Call status UI |
| **Phase E: Notifications** | NotificationController, AlertController, ProximityService, SignalR alert flow |
| **Phase F: Analytics** | Chart.js dashboard, Heatmap visualization, Reports page, Admin tools |
| **Phase G: Polish** | Weather widget, User profile, Loading/error states, Mobile responsive |

---

**Plan complete and saved to `docs/superpowers/plans/2026-05-10-phase-a-incident-system.md`.**

**Two execution options:**

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach do you prefer?**
