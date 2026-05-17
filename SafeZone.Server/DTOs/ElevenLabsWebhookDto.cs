using System.Text.Json.Serialization;

namespace SafeZone.Server.DTOs;

public class ElevenLabsWebhookPayload
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; init; } = string.Empty;

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; init; } = string.Empty;

    [JsonPropertyName("caller_phone_number")]
    public string? CallerPhoneNumber { get; init; }

    [JsonPropertyName("start_time_unix_ms")]
    public long StartTimeUnixMs { get; init; }

    [JsonPropertyName("call_duration_secs")]
    public int? CallDurationSecs { get; init; }

    [JsonPropertyName("transcript")]
    public List<ElevenLabsTranscriptEntry>? Transcript { get; init; }

    [JsonPropertyName("analysis")]
    public ElevenLabsAnalysis? Analysis { get; init; }

    [JsonPropertyName("dynamic_variables")]
    public Dictionary<string, string>? DynamicVariables { get; init; }

    [JsonPropertyName("conversation_initiation_client_data")]
    public ElevenLabsConversationMeta? ConversationMeta { get; init; }
}

public class ElevenLabsTranscriptEntry
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("time_in_call_secs")]
    public int TimeInCallSecs { get; init; }
}

public class ElevenLabsAnalysis
{
    [JsonPropertyName("transcript_summary")]
    public string? TranscriptSummary { get; init; }

    [JsonPropertyName("call_successful")]
    public string? CallSuccessful { get; init; }
}

public class ElevenLabsConversationMeta
{
    [JsonPropertyName("dynamic_variables")]
    public ElevenLabsDynamicVariables? DynamicVariables { get; init; }
}

public class ElevenLabsDynamicVariables
{
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("severity")]
    public string? Severity { get; init; }

    [JsonPropertyName("is_anonymous")]
    public string? IsAnonymous { get; init; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; init; }

    [JsonPropertyName("caller_name")]
    public string? CallerName { get; init; }
}

public class ElevenLabsWebhookResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? IncidentId { get; init; }
    public string? IncidentNumber { get; init; }
}
