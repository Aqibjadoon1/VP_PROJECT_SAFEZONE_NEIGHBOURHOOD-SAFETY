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
    private readonly IGmailNotificationService? _gmail;
    private readonly ISlackNotificationService? _slack;
    private readonly ILogger<SosService>? _logger;

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
        IVoiceCallService voiceCallService,
        IGmailNotificationService? gmail = null,
        ISlackNotificationService? slack = null,
        ILogger<SosService>? logger = null)
    {
        _context = context;
        _config = config;
        _voiceCallService = voiceCallService;
        _gmail = gmail;
        _slack = slack;
        _logger = logger;
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
            ?? await _context.IncidentCategories.FirstOrDefaultAsync();

        var categoryId = category?.CategoryId ?? Guid.Empty;

        var incidentNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}-{Random.Shared.Next(1000, 9999)}";

        var incident = new Incident
        {
            IncidentId = Guid.NewGuid(),
            IncidentNumber = incidentNumber,
            CategoryId = categoryId,
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
            DurationSeconds = Random.Shared.Next(30, 120),
            IsFalseAlarm = false,
            SmsStatus = "sent_mock"
        };

        _context.AICallLogs.Add(callLog);
        await _context.SaveChangesAsync();

        if (_gmail != null)
        {
            var recipientEmail = user.Email ?? user.UserName;
            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var sent = await _gmail.SendIncidentAlertAsync(recipientEmail, $"SOS: {emergencyName}", "Critical");
                        if (!sent)
                        {
                            _logger?.LogWarning("[SOS Notification] Gmail alert was not sent to {Email}. Check Gmail API configuration.", recipientEmail);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to send Gmail SOS alert to {Email}.", recipientEmail);
                    }
                });
            }
            else
            {
                _logger?.LogWarning("[SOS Notification] Cannot send email: user {UserId} has no email address.", userId);
            }
        }

        if (_slack != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _slack.SendAlertAsync(
                        $"SOS EMERGENCY: {emergencyName}",
                        $"SOS triggered by {user.FullName} ({user.PhoneNumber}). Location: ({dto.Latitude:F4}, {dto.Longitude:F4}). AI Script: {aiScript[..Math.Min(aiScript.Length, 200)]}",
                        "Critical");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to send Slack SOS alert.");
                }
            });
        }

        // Notify all superadmins
        _ = Task.Run(async () =>
        {
            try
            {
                var superAdmins = await _context.Users
                    .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive)
                    .ToListAsync();

                foreach (var admin in superAdmins)
                {
                    var adminEmail = admin.Email ?? admin.UserName;
                    if (!string.IsNullOrWhiteSpace(adminEmail) && _gmail != null)
                    {
                        await _gmail.SendEmailAsync(adminEmail,
                            $"[CRITICAL] SOS Emergency: {emergencyName}",
                            $"An SOS emergency has been triggered.\n\nType: {emergencyName}\nTriggered by: {user.FullName} ({user.PhoneNumber})\nLocation: {dto.Latitude:F6}, {dto.Longitude:F6}\nTime: {DateTime.UtcNow:MMM dd, yyyy HH:mm:ss UTC}\n\nView at: http://localhost:5000/authority/sos-logs");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send superadmin SOS notifications.");
            }
        });

        if (IsMockMode)
        {
            var emergencyPrompt = GenerateEmergencyPrompt(dto.EmergencyType, dto.Latitude, dto.Longitude, emergencyName);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _voiceCallService.StartOutboundCallAsync(
                        emergencyNumber,
                        emergencyPrompt,
                        userId);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Voice call failed for SOS emergency.");
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
            .AsNoTracking()
            .Include(c => c.Incident)
            .ThenInclude(i => i!.Reporter)
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
                    : null,
                UserId = c.TriggeredByUserId
            })
            .ToListAsync();
    }

    public async Task<List<SosCallLogDto>> GetAllCallLogsAsync(CallStatus? status = null)
    {
        var query = _context.AICallLogs
            .AsNoTracking()
            .Include(c => c.Incident)
            .ThenInclude(i => i!.Reporter)
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
                    : null,
                UserId = c.TriggeredByUserId
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
            .AsNoTracking()
            .Include(c => c.Incident)
            .ThenInclude(i => i!.Reporter)
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
                : null,
            UserId = log.TriggeredByUserId
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
        var localTime = GetPakistanTime(now);

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

    private static DateTime GetPakistanTime(DateTime utcTime)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            return utcTime.AddHours(5);
        }
    }
}
