using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Helpers;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public class AlertService : IAlertService
{
    private readonly SafeZoneDbContext _context;

    public AlertService(SafeZoneDbContext context)
    {
        _context = context;
    }

    public async Task<AlertResponseDto> CreateAlertAsync(CreateAlertDto dto, Guid issuedById)
    {
        var issuer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == issuedById);

        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            IssuedByAuthorityId = issuedById,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            Scope = dto.Scope,
            RadiusKm = dto.Scope == AlertScope.Radius ? dto.RadiusKm : null,
            CenterLat = dto.Scope == AlertScope.Radius ? dto.CenterLat : null,
            CenterLng = dto.Scope == AlertScope.Radius ? dto.CenterLng : null,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresInMinutes.HasValue 
                ? DateTime.UtcNow.AddMinutes(dto.ExpiresInMinutes.Value) 
                : DateTime.UtcNow.AddHours(24),
            IsActive = true,
            ScheduledAt = dto.ScheduledAt
        };

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        return MapToResponse(alert, issuer?.FullName);
    }

    public async Task<AlertResponseDto?> GetAlertByIdAsync(Guid alertId)
    {
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.AlertId == alertId);

        if (alert == null) return null;

        var issuer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == alert.IssuedByAuthorityId);

        return MapToResponse(alert, issuer?.FullName);
    }

    public async Task<List<AlertListDto>> GetActiveAlertsAsync()
    {
        var now = DateTime.UtcNow;
        
        return await _context.Alerts
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderByDescending(a => a.IssuedAt)
            .Select(a => new AlertListDto
            {
                AlertId = a.AlertId,
                Title = a.Title,
                Type = a.Type,
                Scope = a.Scope,
                IssuedAt = a.IssuedAt,
                ExpiresAt = a.ExpiresAt,
                IsActive = a.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<AlertListDto>> GetAllAlertsAsync()
    {
        return await _context.Alerts
            .OrderByDescending(a => a.IssuedAt)
            .Select(a => new AlertListDto
            {
                AlertId = a.AlertId,
                Title = a.Title,
                Type = a.Type,
                Scope = a.Scope,
                IssuedAt = a.IssuedAt,
                ExpiresAt = a.ExpiresAt,
                IsActive = a.IsActive
            })
            .ToListAsync();
    }

    public async Task<AlertResponseDto?> DeactivateAlertAsync(Guid alertId, Guid deactivatedById)
    {
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.AlertId == alertId);

        if (alert == null) return null;

        alert.IsActive = false;
        await _context.SaveChangesAsync();

        var issuer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == alert.IssuedByAuthorityId);

        return MapToResponse(alert, issuer?.FullName);
    }

    public async Task<List<AlertListDto>> GetAlertsForLocationAsync(double lat, double lng, double radiusKm = 2.0)
    {
        var now = DateTime.UtcNow;
        
        var citywideAlerts = await _context.Alerts
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now) && a.Scope == AlertScope.Citywide)
            .OrderByDescending(a => a.IssuedAt)
            .ToListAsync();

        var radiusAlerts = await _context.Alerts
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now) && a.Scope == AlertScope.Radius)
            .ToListAsync();

        var nearbyRadiusAlerts = radiusAlerts
            .Where(a => GeoHelper.CalculateDistanceKm(
                lat, lng, 
                a.CenterLat ?? 0, a.CenterLng ?? 0) <= (a.RadiusKm ?? 2.0))
            .ToList();

        var allAlerts = citywideAlerts
            .Concat(nearbyRadiusAlerts)
            .OrderByDescending(a => a.IssuedAt)
            .Select(a => new AlertListDto
            {
                AlertId = a.AlertId,
                Title = a.Title,
                Type = a.Type,
                Scope = a.Scope,
                IssuedAt = a.IssuedAt,
                ExpiresAt = a.ExpiresAt,
                IsActive = a.IsActive
            })
            .ToList();

        return allAlerts;
    }

    private AlertResponseDto MapToResponse(Alert alert, string? issuerName)
    {
        return new AlertResponseDto
        {
            AlertId = alert.AlertId,
            Title = alert.Title,
            Message = alert.Message,
            Type = alert.Type,
            Scope = alert.Scope,
            RadiusKm = alert.RadiusKm,
            CenterLat = alert.CenterLat,
            CenterLng = alert.CenterLng,
            IssuedAt = alert.IssuedAt,
            ExpiresAt = alert.ExpiresAt,
            IsActive = alert.IsActive,
            IssuedByAuthorityId = alert.IssuedByAuthorityId,
            IssuedByName = issuerName
        };
    }
}
