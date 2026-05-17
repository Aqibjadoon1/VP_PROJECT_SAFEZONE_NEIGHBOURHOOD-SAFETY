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
