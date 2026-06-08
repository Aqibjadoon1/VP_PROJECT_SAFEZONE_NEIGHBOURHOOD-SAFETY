using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;
using System.Text.Json;
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
    private readonly IConfiguration _configuration;

    public ElevenLabsWebhookController(
        IIncidentService incidentService,
        IHubContext<MapHub> mapHub,
        ILogger<ElevenLabsWebhookController> logger,
        IConfiguration configuration)
    {
        _incidentService = incidentService;
        _mapHub = mapHub;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveIncidentReport([FromBody] ElevenLabsWebhookPayload payload)
    {
        if (!IsSignatureValid(Request))
        {
            _logger.LogWarning("ElevenLabs webhook rejected: invalid signature.");
            return Unauthorized(new { message = "Invalid signature." });
        }

        try
        {
            _logger.LogInformation(
                "ElevenLabs webhook received. AgentId={AgentId}, ConversationId={ConversationId}, Phone={Phone}",
                payload.AgentId, payload.ConversationId, payload.CallerPhoneNumber);

            return await ProcessAndCreateIncident(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ElevenLabs webhook");
            return StatusCode(500, new ElevenLabsWebhookResponse
            {
                Success = false,
                Message = "Processing failed."
            });
        }
    }

    [HttpPost("tool-call")]
    public async Task<IActionResult> ReceiveToolCall([FromBody] JsonElement rawPayload)
    {
        if (!IsSignatureValid(Request))
        {
            _logger.LogWarning("ElevenLabs tool call rejected: invalid signature.");
            return Unauthorized(new { message = "Invalid signature." });
        }

        try
        {
            _logger.LogInformation("ElevenLabs tool call received: {Payload}", rawPayload.ToString());

            var agentId = "";
            var conversationId = "";
            string? callerPhone = null;
            Dictionary<string, string>? dynamicVars = null;

            if (rawPayload.TryGetProperty("agent_id", out var aid))
                agentId = aid.GetString() ?? "";

            if (rawPayload.TryGetProperty("conversation_id", out var cid))
                conversationId = cid.GetString() ?? "";

            if (rawPayload.TryGetProperty("caller_phone_number", out var phone))
                callerPhone = phone.GetString();

            if (rawPayload.TryGetProperty("arguments", out var args))
            {
                var argsStr = args.ValueKind == JsonValueKind.String
                    ? args.GetString() ?? "{}"
                    : args.ToString();
                dynamicVars = JsonSerializer.Deserialize<Dictionary<string, string>>(argsStr);
            }

            if (rawPayload.TryGetProperty("dynamic_variables", out var dv))
            {
                dynamicVars = JsonSerializer.Deserialize<Dictionary<string, string>>(dv.ToString());
            }

            var payload = new ElevenLabsWebhookPayload
            {
                AgentId = agentId,
                ConversationId = conversationId,
                CallerPhoneNumber = callerPhone,
                DynamicVariables = dynamicVars
            };

            return await ProcessAndCreateIncident(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ElevenLabs tool call");
            return StatusCode(500, new ElevenLabsWebhookResponse
            {
                Success = false,
                Message = "Tool call processing failed."
            });
        }
    }

    private bool IsSignatureValid(HttpRequest request)
    {
        var secret = _configuration["ElevenLabs:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
            var allowUnsignedServerTools = _configuration.GetValue<bool>("ElevenLabs:AllowUnsignedServerTools");
            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase) && !allowUnsignedServerTools)
            {
                _logger.LogError("ElevenLabs webhook rejected: WebhookSecret is not configured in production.");
                return false;
            }
            _logger.LogWarning(
                "ElevenLabs webhook accepted without signature verification. Environment={Environment}, AllowUnsignedServerTools={AllowUnsignedServerTools}",
                env,
                allowUnsignedServerTools);
            return true;
        }

        // Placeholder for HMAC validation. In production, compute HMAC-SHA256 of the
        // raw request body using the shared secret and compare with the X-ElevenLabs-Signature header.
        request.Headers.TryGetValue("X-ElevenLabs-Signature", out var signature);
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        // TODO: Implement actual HMAC verification when ElevenLabs provides documentation.
        return true;
    }

    private async Task<IActionResult> ProcessAndCreateIncident(ElevenLabsWebhookPayload payload)
    {
        try
        {
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
                Latitude = lat ?? 0,
                Longitude = lng ?? 0,
                Address = dynamicVars.Address ?? "Reported via ElevenLabs Voice Agent",
                IsAnonymous = isAnonymous,
                IncidentDateTime = DateTime.UtcNow
            };

            var incident = await _incidentService.CreateIncidentAsync(createDto, reporterId: null);

            try
            {
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
            }
            catch (Exception hubEx)
            {
                _logger.LogError(hubEx, "Failed to broadcast ReportNewIncident to SignalR hub from ElevenLabs webhook");
            }

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
                Message = "Processing failed."
            });
        }
    }

    private static ElevenLabsDynamicVariables ResolveDynamicVariables(ElevenLabsWebhookPayload payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.Category)
            || !string.IsNullOrWhiteSpace(payload.Description)
            || !string.IsNullOrWhiteSpace(payload.Address)
            || !string.IsNullOrWhiteSpace(payload.Severity)
            || payload.IsAnonymous.HasValue
            || payload.Latitude.HasValue
            || payload.Longitude.HasValue)
        {
            return new ElevenLabsDynamicVariables
            {
                Category = payload.Category,
                Description = payload.Description,
                Address = payload.Address,
                Severity = payload.Severity,
                IsAnonymous = payload.IsAnonymous?.ToString(),
                Latitude = payload.Latitude?.ToString(CultureInfo.InvariantCulture),
                Longitude = payload.Longitude?.ToString(CultureInfo.InvariantCulture)
            };
        }

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

    private static (double? lat, double? lng) ResolveLocation(ElevenLabsDynamicVariables vars)
    {
        if (double.TryParse(vars.Latitude, out var lat) && double.TryParse(vars.Longitude, out var lng)
            && lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180)
        {
            return (lat, lng);
        }

        return (null, null);
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
