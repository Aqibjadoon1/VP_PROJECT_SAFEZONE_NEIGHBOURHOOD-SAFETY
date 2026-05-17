using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public interface ISosService
{
    Task<SosResponseDto> TriggerEmergencyAsync(TriggerSosDto dto, Guid userId);
    Task<List<SosCallLogDto>> GetMyCallLogsAsync(Guid userId);
    Task<List<SosCallLogDto>> GetAllCallLogsAsync(CallStatus? status = null);
    Task<SosCallLogDto?> MarkAsFalseAlarmAsync(Guid logId, Guid userId);
    Task<SosCallLogDto?> GetCallLogByIdAsync(Guid logId);
    
    bool IsMockMode { get; }
}
