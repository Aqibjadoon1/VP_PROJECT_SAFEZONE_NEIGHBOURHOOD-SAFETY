using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

  public class SosService : ISosService
{
    private readonly SafeZoneDbContext _context;
    private readonly IConfiguration _config;
    private readonly IVoiceCallService _voiceCallService;

     public bool IsMockMode {
        get {
            var mockMode = _config["Twilio:UseMockMode"];
            if (string.IsNullOrEmpty(mockMode)) return true;
            return bool.TryParse(mockMode, out var result) ? result : true;
        }
    }

    private static readonly Dictionary<AuthorityType, string> EmergencyNumbers = new()
    {
        { AuthorityType.Police, "15" },
        { AuthorityType.Ambulance, "115" },
        { AuthorityType.FireBrigade, "16" },
        { AuthorityType.TrafficPolice, "1915" }
    };

    private static readonly Dictionary<AuthorityType, string> EmergencyNames = new()
    {
        { AuthorityType.Police, "Police Emergency" },
        { AuthorityType.Ambulance, "Ambulance Service" },
        { AuthorityType.FireBrigade, "Fire Brigade" },
        { AuthorityType.TrafficPolice, "Traffic Police" }
    };

    public SosService(
        SafeZoneDbContext context, 
        IConfiguration config,
        IVoiceCallService voiceCallService)
    {
        _context = context;
        _config = config;
        _voiceCallService = voiceCallService;
    }

     public async Task<SosResponseDto> TriggerEmergencyAsync(TriggerSosDto dto, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var emergencyNumber = EmergencyNumbers.GetValueOrDefault(dto.EmergencyType, "15");
        var emergencyName = EmergencyNames.GetValueOrDefault(dto.EmergencyType, "Emergency");

        var aiScript = GenerateEmergencyScript(
            dto.EmergencyType,
            dto.Latitude,
            dto.Longitude,
            user.FullName,
            user.PhoneNumber,
            dto.AdditionalNotes
        );

        var category = await _context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Name == "Assault" || c.Name == "Other")
            ?? await _context.IncidentCategories.FirstAsync();

        var incidentNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var incident = new Incident
        {
            IncidentId = Guid.NewGuid(),
            IncidentNumber = incidentNumber,
            CategoryId = category.CategoryId,
            Title = $"SOS: {emergencyName} Emergency",
            Description = aiScript,
            Severity = SeverityLevel.Critical,
            Status = IncidentStatus.Pending,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = $"Emergency at ({dto.Latitude:F4}, {dto.Longitude:F4})",
            ReporterId = userId,
            ReportedAt = DateTime.UtcNow,
            IncidentDateTime = DateTime.UtcNow,
            IsAnonymous = false
        };

        _context.Incidents.Add(incident);

        var callLog = new AICallLog
        {
            LogId = Guid.NewGuid(),
            IncidentId = incident.IncidentId,
            TriggeredByUserId = userId,
            CallType = dto.EmergencyType.ToString(),
            PhoneNumberCalled = emergencyNumber,
            CalledNumbers = emergencyNumber,
            AIScript = aiScript,
            Status = CallStatus.Completed,
            InitiatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            DurationSeconds = new Random().Next(30, 120),
            IsFalseAlarm = false,
            SmsStatus = "sent_mock"
        };

         _context.AICallLogs.Add(callLog);
        await _context.SaveChangesAsync();

        if (IsMockMode)
        {
            var emergencyPrompt = GenerateEmergencyPrompt(dto.EmergencyType, dto.Latitude, dto.Longitude, emergencyName);
            
            _ = Task.Run(async () =>
            {
                try
                {
                    var callSession = await _voiceCallService.StartOutboundCallAsync(
                        emergencyNumber,
                        emergencyPrompt,
                        userId);
                }
                catch
                {
                }
            });
        }

        return new SosResponseDto
        {
            CallLogId = callLog.LogId,
            CallType = dto.EmergencyType.ToString(),
            Status = CallStatus.Completed,
            InitiatedAt = callLog.InitiatedAt,
            PhoneNumberCalled = emergencyNumber,
            AIScript = aiScript,
            IsMockMode = true,
            Message = $"MOCK: Emergency call to {emergencyName} ({emergencyNumber}) simulated. A critical incident has been created. In production, this would trigger the configured voice provider and LLM assistant."
        };
    }

     public async Task<List<SosCallLogDto>> GetMyCallLogsAsync(Guid userId)
    {
        return await _context.AICallLogs
            .Include(c => c.Incident)
            .Where(c => c.TriggeredByUserId == userId)
            .OrderByDescending(c => c.InitiatedAt)
            .Select(c => new SosCallLogDto
            {
                LogId = c.LogId,
                CallType = c.CallType,
                PhoneNumberCalled = c.PhoneNumberCalled,
                CalledNumbers = c.CalledNumbers,
                Status = c.Status,
                InitiatedAt = c.InitiatedAt,
                CompletedAt = c.CompletedAt,
                DurationSeconds = c.DurationSeconds,
                IsFalseAlarm = c.IsFalseAlarm,
                AIScript = c.AIScript,
                IncidentLatitude = c.Incident != null ? c.Incident.Latitude : 0,
                IncidentLongitude = c.Incident != null ? c.Incident.Longitude : 0,
                IncidentTitle = c.Incident != null ? c.Incident.Title : null,
                TriggeredByUserName = c.Incident != null && c.Incident.Reporter != null 
                    ? c.Incident.Reporter.FullName 
                    : null
            })
            .ToListAsync();
    }

     public async Task<List<SosCallLogDto>> GetAllCallLogsAsync(CallStatus? status = null)
    {
        var query = _context.AICallLogs
            .Include(c => c.Incident)
            .ThenInclude(i => i.Reporter)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query
            .OrderByDescending(c => c.InitiatedAt)
            .Select(c => new SosCallLogDto
            {
                LogId = c.LogId,
                CallType = c.CallType,
                PhoneNumberCalled = c.PhoneNumberCalled,
                CalledNumbers = c.CalledNumbers,
                Status = c.Status,
                InitiatedAt = c.InitiatedAt,
                CompletedAt = c.CompletedAt,
                DurationSeconds = c.DurationSeconds,
                IsFalseAlarm = c.IsFalseAlarm,
                AIScript = c.AIScript,
                IncidentLatitude = c.Incident != null ? c.Incident.Latitude : 0,
                IncidentLongitude = c.Incident != null ? c.Incident.Longitude : 0,
                IncidentTitle = c.Incident != null ? c.Incident.Title : null,
                TriggeredByUserName = c.Incident != null && c.Incident.Reporter != null 
                    ? c.Incident.Reporter.FullName 
                    : null
            })
            .ToListAsync();
    }

    public async Task<SosCallLogDto?> MarkAsFalseAlarmAsync(Guid logId, Guid userId)
    {
        var log = await _context.AICallLogs
            .FirstOrDefaultAsync(c => c.LogId == logId && c.TriggeredByUserId == userId);

        if (log == null) return null;

        log.IsFalseAlarm = true;
        await _context.SaveChangesAsync();

        return await GetCallLogByIdAsync(logId);
    }

     public async Task<SosCallLogDto?> GetCallLogByIdAsync(Guid logId)
    {
        var log = await _context.AICallLogs
            .Include(c => c.Incident)
            .ThenInclude(i => i.Reporter)
            .FirstOrDefaultAsync(c => c.LogId == logId);

        if (log == null) return null;

        return new SosCallLogDto
        {
            LogId = log.LogId,
            CallType = log.CallType,
            PhoneNumberCalled = log.PhoneNumberCalled,
            CalledNumbers = log.CalledNumbers,
            Status = log.Status,
            InitiatedAt = log.InitiatedAt,
            CompletedAt = log.CompletedAt,
            DurationSeconds = log.DurationSeconds,
            IsFalseAlarm = log.IsFalseAlarm,
            AIScript = log.AIScript,
            IncidentLatitude = log.Incident != null ? log.Incident.Latitude : 0,
            IncidentLongitude = log.Incident != null ? log.Incident.Longitude : 0,
            IncidentTitle = log.Incident != null ? log.Incident.Title : null,
            TriggeredByUserName = log.Incident != null && log.Incident.Reporter != null 
                ? log.Incident.Reporter.FullName 
                : null
         };
    }

    private string GenerateEmergencyPrompt(
        AuthorityType emergencyType,
        double latitude,
        double longitude,
        string emergencyName)
    {
        return $"You are the SafeZone AI Emergency Assistant. Calling {emergencyName} services. " +
               $"Emergency location: coordinates ({latitude:F6}, {longitude:F6}). " +
               $"Be calm, professional, and gather critical info: number of people, hazards, medical conditions. " +
               $"Keep responses concise.";
    }

    private string GenerateEmergencyScript(
        AuthorityType emergencyType,
        double latitude,
        double longitude,
        string? userName,
        string? userPhone,
        string? additionalNotes)
    {
        var emergencyName = EmergencyNames.GetValueOrDefault(emergencyType, "Emergency");
        var now = DateTime.UtcNow;
        var localTime = now.AddHours(5);

        var scriptLines = new List<string>
        {
            $"=== AI EMERGENCY CALL SCRIPT ===",
            $"",
            $"CALL TYPE: {emergencyName}",
            $"TIME (UTC): {now:yyyy-MM-dd HH:mm:ss}",
            $"TIME (PKT): {localTime:yyyy-MM-dd HH:mm:ss}",
            $"",
            $"=== CALLER INFORMATION ===",
            $"Name: {userName ?? "Anonymous"}",
            $"Phone: {userPhone ?? "N/A"}",
            $"",
            $"=== LOCATION ===",
            $"Latitude: {latitude:F6}",
            $"Longitude: {longitude:F6}",
            $"Google Maps: https://www.google.com/maps?q={latitude},{longitude}",
            $"",
            $"=== EMERGENCY DETAILS ==="
        };

        switch (emergencyType)
        {
            case AuthorityType.Police:
                scriptLines.AddRange(new[]
                {
                    $"This is a POLICE EMERGENCY call from SafeZone.",
                    $"The caller reports needing immediate police assistance.",
                    $"Please dispatch officers to the coordinates provided.",
                    $"The location may be unsafe - approach with caution."
                });
                break;

            case AuthorityType.Ambulance:
                scriptLines.AddRange(new[]
                {
                    $"This is a MEDICAL EMERGENCY call from SafeZone.",
                    $"The caller reports needing urgent medical assistance.",
                    $"Please dispatch an ambulance to the coordinates provided.",
                    $"The caller may be in distress - respond urgently."
                });
                break;

            case AuthorityType.FireBrigade:
                scriptLines.AddRange(new[]
                {
                    $"This is a FIRE EMERGENCY call from SafeZone.",
                    $"The caller reports a fire or fire-related emergency.",
                    $"Please dispatch fire brigade to the coordinates provided.",
                    $"Ensure full emergency response - lives may be at risk."
                });
                break;

            case AuthorityType.TrafficPolice:
                scriptLines.AddRange(new[]
                {
                    $"This is a TRAFFIC EMERGENCY call from SafeZone.",
                    $"The caller reports a traffic accident or road emergency.",
                    $"Please dispatch traffic police to the coordinates provided.",
                    $"There may be injuries or road blockage."
                });
                break;
        }

        if (!string.IsNullOrWhiteSpace(additionalNotes))
        {
            scriptLines.Add("");
            scriptLines.Add("=== ADDITIONAL NOTES FROM CALLER ===");
            scriptLines.Add(additionalNotes);
        }

        scriptLines.Add("");
        scriptLines.Add("=== INSTRUCTIONS FOR RESPONDER ===");
        scriptLines.Add("1. Proceed to the given coordinates immediately");
        scriptLines.Add("2. Attempt to contact the caller upon arrival");
        scriptLines.Add("3. Assess the situation and provide appropriate assistance");
        scriptLines.Add("4. Update the incident status in SafeZone system");
        scriptLines.Add("");
        scriptLines.Add("=== END OF AI SCRIPT ===");
        scriptLines.Add($"(Generated by SafeZone AI Emergency Agent at {now:HH:mm:ss})");

        return string.Join(Environment.NewLine, scriptLines);
    }
}
