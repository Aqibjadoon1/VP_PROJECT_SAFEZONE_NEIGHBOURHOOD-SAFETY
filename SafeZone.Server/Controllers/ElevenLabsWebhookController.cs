using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SafeZone.Server.DTOs;
using SafeZone.Server.Hubs;
using SafeZone.Server.Models;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ElevenLabsWebhookController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly IHubContext<MapHub> _mapHub;
    private readonly ILogger<ElevenLabsWebhookController> _logger;

    public ElevenLabsWebhookController(
        IIncidentService incidentService,
        IHubContext<MapHub> mapHub,
        ILogger<ElevenLabsWebhookController> logger)
    {
        _incidentService = incidentService;
        _mapHub = mapHub;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveIncidentReport([FromBody] ElevenLabsWebhookPayload payload)
    {
        try
        {
            _logger.LogInformation(
                "ElevenLabs webhook received. AgentId={AgentId}, ConversationId={ConversationId}, Phone={Phone}",
                payload.AgentId, payload.ConversationId, payload.CallerPhoneNumber);

            var dynamicVars = ResolveDynamicVariables(payload);
            var categoryId = await ResolveCategoryAsync(dynamicVars.Category, payload);
            var title = ResolveTitle(dynamicVars.Category);
            var description = ResolveDescription(dynamicVars.Description, payload);
            var severity = ResolveSeverity(dynamicVars.Severity);
            var (lat, lng) = ResolveLocation(dynamicVars);
            var isAnonymous = ResolveIsAnonymous(dynamicVars.IsAnonymous);

            if (categoryId == Guid.Empty)
            {
                categoryId = await GetFallbackCategoryIdAsync();
            }

            var createDto = new CreateIncidentDto
            {
                CategoryId = categoryId,
                Title = title,
                Description = description,
                Severity = severity,
                Latitude = lat,
                Longitude = lng,
                Address = dynamicVars.Address ?? "Reported via ElevenLabs Voice Agent",
                IsAnonymous = isAnonymous,
                IncidentDateTime = DateTime.UtcNow
            };

            var incident = await _incidentService.CreateIncidentAsync(createDto, reporterId: null);

            await _mapHub.Clients.All.SendAsync("ReportNewIncident", new
            {
                incident.IncidentId,
                incident.Title,
                incident.CategoryName,
                incident.Latitude,
                incident.Longitude,
                incident.Severity,
                incident.Status,
                incident.ReportedAt
            });

            _logger.LogInformation(
                "Incident created from ElevenLabs webhook. Id={IncidentId}, Number={Number}, Category={Category}",
                incident.IncidentId, incident.IncidentNumber, dynamicVars.Category);

            return Ok(new ElevenLabsWebhookResponse
            {
                Success = true,
                Message = "Incident received and logged.",
                IncidentId = incident.IncidentId.ToString(),
                IncidentNumber = incident.IncidentNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ElevenLabs webhook");
            return StatusCode(500, new ElevenLabsWebhookResponse
            {
                Success = false,
                Message = $"Processing failed: {ex.Message}"
            });
        }
    }

    private static ElevenLabsDynamicVariables ResolveDynamicVariables(ElevenLabsWebhookPayload payload)
    {
        if (payload.DynamicVariables is { Count: > 0 })
        {
            return new ElevenLabsDynamicVariables
            {
                Category = payload.DynamicVariables.GetValueOrDefault("category"),
                Description = payload.DynamicVariables.GetValueOrDefault("description"),
                Address = payload.DynamicVariables.GetValueOrDefault("address"),
                Severity = payload.DynamicVariables.GetValueOrDefault("severity"),
                IsAnonymous = payload.DynamicVariables.GetValueOrDefault("is_anonymous"),
                Latitude = payload.DynamicVariables.GetValueOrDefault("latitude"),
                Longitude = payload.DynamicVariables.GetValueOrDefault("longitude"),
                CallerName = payload.DynamicVariables.GetValueOrDefault("caller_name")
            };
        }

        return payload.ConversationMeta?.DynamicVariables ?? new ElevenLabsDynamicVariables();
    }

    private async Task<Guid> ResolveCategoryAsync(string? category, ElevenLabsWebhookPayload payload)
    {
        var categoryName = category ?? "";
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            categoryName = DeriveCategoryFromTranscript(payload.Analysis?.TranscriptSummary ?? "");
        }

        var categories = await _incidentService.GetCategoriesAsync();
        var match = categories.FirstOrDefault(c =>
            c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        return match?.CategoryId ?? Guid.Empty;
    }

    private static string DeriveCategoryFromTranscript(string summary)
    {
        summary = summary.ToLowerInvariant();
        if (summary.Contains("fire") || summary.Contains("burn")) return "Fire";
        if (summary.Contains("theft") || summary.Contains("stolen") || summary.Contains("steal")) return "Theft";
        if (summary.Contains("vandal")) return "Vandalism";
        if (summary.Contains("assault") || summary.Contains("attack") || summary.Contains("fight")) return "Assault";
        if (summary.Contains("accident") || summary.Contains("crash") || summary.Contains("collision")) return "Accident";
        if (summary.Contains("robbery") || summary.Contains("rob")) return "Robbery";
        if (summary.Contains("shoot") || summary.Contains("gun") || summary.Contains("shot")) return "Shooting";
        if (summary.Contains("medical") || summary.Contains("hurt") || summary.Contains("injur")) return "Medical Emergency";
        if (summary.Contains("harass") || summary.Contains("stalk")) return "Sexual Harassment";
        return "Suspicious Activity";
    }

    private static string ResolveTitle(string? category)
    {
        var cat = category ?? string.Empty;
        if (cat.Length > 0)
        {
            cat = char.ToUpperInvariant(cat[0]) + cat[1..];
        }
        else
        {
            cat = "Incident";
        }

        return $"{cat} – Voice Agent Report";
    }

    private static string ResolveDescription(string? description, ElevenLabsWebhookPayload payload)
    {
        var desc = description
            ?? payload.Analysis?.TranscriptSummary
            ?? "Incident reported via ElevenLabs voice agent.";

        if (!string.IsNullOrWhiteSpace(payload.CallerPhoneNumber))
        {
            desc += $"\nCaller Phone: {payload.CallerPhoneNumber}";
        }

        if (!string.IsNullOrWhiteSpace(payload.ConversationId))
        {
            desc += $"\nConversation ID: {payload.ConversationId}";
        }

        return desc.Trim();
    }

    private static SeverityLevel ResolveSeverity(string? severity)
    {
        return (severity ?? "").ToLowerInvariant() switch
        {
            "critical" => SeverityLevel.Critical,
            "high" => SeverityLevel.High,
            "low" => SeverityLevel.Low,
            _ => SeverityLevel.Medium
        };
    }

    private static (double lat, double lng) ResolveLocation(ElevenLabsDynamicVariables vars)
    {
        if (double.TryParse(vars.Latitude, out var lat) && double.TryParse(vars.Longitude, out var lng)
            && lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180)
        {
            return (lat, lng);
        }

        return (33.6844, 73.0479);
    }

    private static bool ResolveIsAnonymous(string? isAnonymous)
    {
        var value = (isAnonymous ?? "").ToLowerInvariant();
        return value == "true" || value == "yes" || value == "1";
    }

    private async Task<Guid> GetFallbackCategoryIdAsync()
    {
        var categories = await _incidentService.GetCategoriesAsync();
        var match = categories.FirstOrDefault(c => c.Name == "Suspicious Activity");
        return match?.CategoryId ?? Guid.Empty;
    }
}
