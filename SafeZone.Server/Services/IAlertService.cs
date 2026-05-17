using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public interface IAlertService
{
    Task<AlertResponseDto> CreateAlertAsync(CreateAlertDto dto, Guid issuedById);
    Task<AlertResponseDto?> GetAlertByIdAsync(Guid alertId);
    Task<List<AlertListDto>> GetActiveAlertsAsync();
    Task<List<AlertListDto>> GetAllAlertsAsync();
    Task<AlertResponseDto?> DeactivateAlertAsync(Guid alertId, Guid deactivatedById);
    Task<List<AlertListDto>> GetAlertsForLocationAsync(double lat, double lng, double radiusKm = 2.0);
}
