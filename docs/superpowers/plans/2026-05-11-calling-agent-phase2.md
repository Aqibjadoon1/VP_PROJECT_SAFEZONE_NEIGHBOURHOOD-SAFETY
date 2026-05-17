# AI Calling Agent - Phase 2: Voice Orchestration Implementation Plan

> **For agentic workers:** Use subagent-driven-development or executing-plans.

**Goal:** Implement the voice orchestration layer with mock audio loop. Connects Phase 1 providers to existing SosService via SignalR.

**Architecture:**
- `VoicePipelineService` - STT→LLM→TTS loop (mock audio for Phase 2)
- `VoiceCallService` - CallSession management, orchestrates pipeline
- `CallHub` - SignalR for real-time UI updates
- SosService integration - triggers mock call on SOS

---

## File Structure

| File | Responsibility |
|------|----------------|
| `DTOs/VoiceCallDtos.cs` | `StartCallDto`, `CallResponseDto`, `TranscriptSegmentDto` |
| `Services/IVoicePipeline.cs` | Pipeline interface |
| `Services/IVoiceCallService.cs` | Call service interface |
| `Services/VoicePipelineService.cs` | Mock pipeline implementation |
| `Services/VoiceCallService.cs` | Call session management |
| `Hubs/CallHub.cs` | SignalR hub |
| `Program.cs` (modified) | DI registrations + hub mapping |
| `Services/SosService.cs` (modified) | Trigger voice call on SOS |

---

## Task List

### Task 1: Create VoiceCallDtos

**Files:** Create: `SafeZone.Server/DTOs/VoiceCallDtos.cs`

**Step 1: Write DTOs**

```csharp
namespace SafeZone.Server.DTOs;

public record StartCallDto
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string? SystemPrompt { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public record CallResponseDto
{
    public Guid CallId { get; init; }
    public string RemoteNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsMockMode { get; init; }
}

public record TranscriptSegmentDto
{
    public string Speaker { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
```

**Step 2: Build verification**

```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Success

---

### Task 2: Create CallSession and TranscriptSegment Models

**Files:** Create: `SafeZone.Server/Services/CallSession.cs` (models in Services namespace following existing pattern)

Note: Alternatively these can be records in the same file as interfaces.

**Step 1: Write the models**

```csharp
namespace SafeZone.Server.Services;

public record CallSession
{
    public Guid CallId { get; init; }
    public string RemoteNumber { get; init; } = string.Empty;
    public CallDirection Direction { get; init; }
    public CallStatus Status { get; set; }
    
    public List<ChatMessage> ConversationHistory { get; init; } = new();
    public List<TranscriptSegment> Transcript { get; init; } = new();
    
    public DateTime CreatedAt { get; init; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    public string? SystemPrompt { get; init; }
    public Guid? TriggeredByUserId { get; init; }
    public Guid? IncidentId { get; set; }
    public bool IsMock { get; init; }
}

public enum CallDirection
{
    Outbound,
    Inbound
}

public record TranscriptSegment
{
    public SpeakerRole Speaker { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public enum SpeakerRole
{
    User,
    Agent
}
```

Note: `ChatMessage`, `ChatRole`, `CallStatus` already exist from Phase 1.

**Step 2: Build verification**

```bash
dotnet build
```

---

### Task 3: Create IVoicePipeline Interface

**Files:** Create: `SafeZone.Server/Services/IVoicePipeline.cs`

**Step 1: Write interface**

```csharp
namespace SafeZone.Server.Services;

public interface IVoicePipeline : IDisposable
{
    Task<string> ProcessTurnAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    Task<(string Transcript, byte[]? ResponseAudio)> ProcessTurnWithAudioAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> SynthesizeTextAsync(string text, CancellationToken cancellationToken = default);

    Task<string> TranscribeAudioAsync(byte[] audio, CancellationToken cancellationToken = default);
}
```

**Step 2: Build verification**

---

### Task 4: Create IVoiceCallService Interface

**Files:** Create: `SafeZone.Server/Services/IVoiceCallService.cs`

**Step 1: Write interface**

```csharp
namespace SafeZone.Server.Services;

public interface IVoiceCallService
{
    Task<CallSession> StartOutboundCallAsync(
        string phoneNumber,
        string? systemPrompt = null,
        Guid? triggeredByUserId = null,
        CancellationToken cancellationToken = default);

    Task<CallSession?> GetCallAsync(Guid callId);

    Task<List<CallSession>> GetActiveCallsAsync();

    Task EndCallAsync(Guid callId, string? reason = null);

    Task<string?> GetFullTranscriptAsync(Guid callId);

    bool IsMockMode { get; }
}
```

**Step 2: Build verification**

---

### Task 5: Create CallHub SignalR Hub

**Files:** Create: `SafeZone.Server/Hubs/CallHub.cs`

**Step 1: Write hub** (follows AlertHub.cs pattern)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SafeZone.Server.Hubs;

[Authorize]
public class CallHub : Hub
{
    public const string AuthoritiesGroup = "call-monitors";

    public async Task JoinCallMonitoring()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AuthoritiesGroup);
    }

    public async Task LeaveCallMonitoring()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AuthoritiesGroup);
    }

    public async Task JoinCallUpdates(Guid callId)
    {
        var groupName = $"call_{callId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveCallUpdates(Guid callId)
    {
        var groupName = $"call_{callId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
```

**Step 2: Build verification**

---

### Task 6: Create VoicePipelineService (Mock Implementation)

**Files:** Create: `SafeZone.Server/Services/VoicePipelineService.cs`

**Step 1: Write implementation** - uses Phase 1 mock providers

```csharp
using Microsoft.Extensions.Logging;

namespace SafeZone.Server.Services;

public class VoicePipelineService : IVoicePipeline
{
    private readonly ISpeechToText _stt;
    private readonly ILanguageModel _llm;
    private readonly ITextToSpeech _tts;
    private readonly ILogger<VoicePipelineService> _logger;

    public bool IsMock => _stt.IsMock || _llm.IsMock || _tts.IsMock;

    public VoicePipelineService(
        ISpeechToText stt,
        ILanguageModel llm,
        ITextToSpeech tts,
        ILogger<VoicePipelineService> logger)
    {
        _stt = stt;
        _llm = llm;
        _tts = tts;
        _logger = logger;
    }

    public async Task<string> ProcessTurnAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ProcessTurnAsync: audioLength={AudioLength}", userAudio.Length);
        
        var transcript = await _stt.TranscribeAsync(userAudio);
        _logger.LogDebug("STT result: {Transcript}", transcript);
        
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return string.Empty;
        }

        var response = await _llm.GenerateResponseAsync(
            transcript,
            conversationHistory,
            systemPrompt,
            cancellationToken);
        
        _logger.LogDebug("LLM response: {Response}", response.Length > 100 ? response[..100] + "..." : response);
        return response;
    }

    public async Task<(string Transcript, byte[]? ResponseAudio)> ProcessTurnWithAudioAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var transcript = await ProcessTurnAsync(userAudio, conversationHistory, systemPrompt, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return (string.Empty, null);
        }

        var latestResponse = conversationHistory
            .LastOrDefault(m => m.Role == ChatRole.Assistant)?.Content;
        
        if (string.IsNullOrWhiteSpace(latestResponse))
        {
            latestResponse = "Understood. I'm processing your request.";
        }

        var audio = await _tts.SynthesizeAsync(latestResponse, cancellationToken);
        return (transcript, audio);
    }

    public Task<byte[]> SynthesizeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return _tts.SynthesizeAsync(text, cancellationToken);
    }

    public Task<string> TranscribeAudioAsync(byte[] audio, CancellationToken cancellationToken = default)
    {
        return _stt.TranscribeAsync(audio);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
```

**Step 2: Build verification**

---

### Task 7: Create VoiceCallService (Call Orchestrator with Mock Loop)

**Files:** Create: `SafeZone.Server/Services/VoiceCallService.cs`

**Step 1: Write implementation** - manages CallSession dictionary, runs mock audio loop

```csharp
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SafeZone.Server.Data;
using SafeZone.Server.Hubs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public class VoiceCallService : IVoiceCallService
{
    private readonly ConcurrentDictionary<Guid, CallSession> _activeCalls = new();
    private readonly IVoicePipeline _pipeline;
    private readonly IHubContext<CallHub> _callHub;
    private readonly SafeZoneDbContext _dbContext;
    private readonly ILogger<VoiceCallService> _logger;

    public bool IsMockMode => true;

    public VoiceCallService(
        IVoicePipeline pipeline,
        IHubContext<CallHub> callHub,
        SafeZoneDbContext dbContext,
        ILogger<VoiceCallService> logger)
    {
        _pipeline = pipeline;
        _callHub = callHub;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CallSession> StartOutboundCallAsync(
        string phoneNumber,
        string? systemPrompt = null,
        Guid? triggeredByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var callId = Guid.NewGuid();
        
        var session = new CallSession
        {
            CallId = callId,
            RemoteNumber = phoneNumber,
            Direction = CallDirection.Outbound,
            Status = CallStatus.Initiated,
            CreatedAt = DateTime.UtcNow,
            SystemPrompt = systemPrompt ?? GetDefaultEmergencyPrompt(),
            TriggeredByUserId = triggeredByUserId,
            IsMock = true
        };

        _activeCalls.TryAdd(callId, session);
        _logger.LogInformation("Started mock outbound call: CallId={CallId}, Number={PhoneNumber}", callId, phoneNumber);

        _ = RunMockCallLoopAsync(session, cancellationToken);

        await BroadcastCallStatusAsync(session);
        await BroadcastNewCallToAuthoritiesAsync(session);

        return session;
    }

    private string GetDefaultEmergencyPrompt()
    {
        return "You are the SafeZone AI Emergency Assistant. You are on a call with emergency services. " +
               "Be professional, calm, and clear. Gather critical information: location, number of people involved, " +
               "any hazards, medical conditions. Keep responses concise and actionable.";
    }

    private async Task RunMockCallLoopAsync(CallSession session, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(800, cancellationToken);
            session.Status = CallStatus.Ringing;
            await BroadcastCallStatusAsync(session);
            _logger.LogDebug("Call ringing: {CallId}", session.CallId);

            await Task.Delay(1200, cancellationToken);
            session.Status = CallStatus.Answered;
            session.ConnectedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);
            _logger.LogDebug("Call connected: {CallId}", session.CallId);

            var openingMessage = "Hello, this is the SafeZone Emergency Assistant calling. We have a report of an emergency at the caller's location.";
            session.Transcript.Add(new TranscriptSegment
            {
                Speaker = SpeakerRole.Agent,
                Text = openingMessage,
                Timestamp = DateTime.UtcNow
            });
            session.ConversationHistory.Add(new ChatMessage(ChatRole.Assistant, openingMessage));
            
            await BroadcastTranscriptAsync(session.CallId, SpeakerRole.Agent, openingMessage);

            var mockUserInputs = new[]
            {
                "Yes, this is an emergency. There's been an accident.",
                "Two people are involved. One is unconscious.",
                "The location is near the main intersection. Coordinates have been sent.",
                "No, no fire. Just injuries from the collision."
            };

            foreach (var userInput in mockUserInputs.Take(3))
            {
                await Task.Delay(1500, cancellationToken);

                session.Transcript.Add(new TranscriptSegment
                {
                    Speaker = SpeakerRole.User,
                    Text = userInput,
                    Timestamp = DateTime.UtcNow
                });
                session.ConversationHistory.Add(new ChatMessage(ChatRole.User, userInput));
                
                await BroadcastTranscriptAsync(session.CallId, SpeakerRole.User, userInput);
                _logger.LogDebug("Mock user input: {Input}", userInput);

                var mockAudio = GenerateMockAudio(16000, 2);
                var aiResponse = await _pipeline.ProcessTurnAsync(
                    mockAudio,
                    session.ConversationHistory,
                    session.SystemPrompt,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    session.Transcript.Add(new TranscriptSegment
                    {
                        Speaker = SpeakerRole.Agent,
                        Text = aiResponse,
                        Timestamp = DateTime.UtcNow
                    });
                    session.ConversationHistory.Add(new ChatMessage(ChatRole.Assistant, aiResponse));
                    
                    await BroadcastTranscriptAsync(session.CallId, SpeakerRole.Agent, aiResponse);
                    await BroadcastAgentSpeakingAsync(session.CallId, isSpeaking: true);
                    await Task.Delay(500, cancellationToken);
                    await BroadcastAgentSpeakingAsync(session.CallId, isSpeaking: false);
                }
            }

            await Task.Delay(1000, cancellationToken);
            
            session.Status = CallStatus.Completed;
            session.EndedAt = DateTime.UtcNow;
            
            await BroadcastCallStatusAsync(session);
            await BroadcastCallEndedAsync(session);

            await UpdateAICallLogAsync(session);

            _logger.LogInformation("Mock call completed: CallId={CallId}, Duration={Duration}s",
                session.CallId, (session.EndedAt - session.ConnectedAt)?.TotalSeconds ?? 0);
        }
        catch (OperationCanceledException)
        {
            session.Status = CallStatus.Cancelled;
            session.EndedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);
            _logger.LogInformation("Call cancelled: {CallId}", session.CallId);
        }
        catch (Exception ex)
        {
            session.Status = CallStatus.Failed;
            session.EndedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);
            _logger.LogError(ex, "Mock call failed: {CallId}", session.CallId);
        }
        finally
        {
            _activeCalls.TryRemove(session.CallId, out _);
        }
    }

    private byte[] GenerateMockAudio(int sampleRate, double durationSeconds)
    {
        var sampleCount = (int)(sampleRate * durationSeconds);
        var bytes = new byte[sampleCount * 2];
        var random = new Random();
        
        for (int i = 0; i < sampleCount; i++)
        {
            var sample = (short)(random.NextDouble() * 100 - 50);
            var offset = i * 2;
            bytes[offset] = (byte)(sample & 0xFF);
            bytes[offset + 1] = (byte)((sample >> 8) & 0xFF);
        }
        
        return bytes;
    }

    public Task<CallSession?> GetCallAsync(Guid callId)
    {
        _activeCalls.TryGetValue(callId, out var session);
        return Task.FromResult(session);
    }

    public Task<List<CallSession>> GetActiveCallsAsync()
    {
        return Task.FromResult(_activeCalls.Values.ToList());
    }

    public async Task EndCallAsync(Guid callId, string? reason = null)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            session.Status = CallStatus.Completed;
            session.EndedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);
            await BroadcastCallEndedAsync(session);
            _activeCalls.TryRemove(callId, out _);
        }
    }

    public Task<string?> GetFullTranscriptAsync(Guid callId)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            var transcript = string.Join("\n", session.Transcript
                .Select(t => $"[{t.Speaker}] {t.Text}"));
            return Task.FromResult<string?>(transcript);
        }
        return Task.FromResult<string?>(null);
    }

    private async Task UpdateAICallLogAsync(CallSession session)
    {
        var log = await _dbContext.AICallLogs
            .OrderByDescending(l => l.InitiatedAt)
            .FirstOrDefaultAsync(l => 
                (session.TriggeredByUserId == null || l.TriggeredByUserId == session.TriggeredByUserId) &&
                l.Status == CallStatus.Completed);

        if (log != null)
        {
            var fullTranscript = await GetFullTranscriptAsync(session.CallId);
            if (session.EndedAt.HasValue && session.ConnectedAt.HasValue)
            {
                log.DurationSeconds = (int)(session.EndedAt - session.ConnectedAt).Value.TotalSeconds;
            }
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task BroadcastCallStatusAsync(CallSession session)
    {
        var callGroup = $"call_{session.CallId}";
        await _callHub.Clients.Group(callGroup).SendAsync("CallStatusUpdated", new
        {
            CallId = session.CallId,
            Status = session.Status.ToString(),
            RemoteNumber = session.RemoteNumber,
            Direction = session.Direction.ToString(),
            session.CreatedAt,
            session.ConnectedAt,
            session.EndedAt
        });
    }

    private async Task BroadcastTranscriptAsync(Guid callId, SpeakerRole speaker, string text)
    {
        var callGroup = $"call_{callId}";
        await _callHub.Clients.Group(callGroup).SendAsync("TranscriptSegment", new
        {
            CallId = callId,
            Speaker = speaker.ToString(),
            Text = text,
            Timestamp = DateTime.UtcNow
        });
    }

    private async Task BroadcastAgentSpeakingAsync(Guid callId, bool isSpeaking)
    {
        var callGroup = $"call_{callId}";
        await _callHub.Clients.Group(callGroup).SendAsync("AgentSpeaking", new
        {
            CallId = callId,
            IsSpeaking = isSpeaking
        });
    }

    private async Task BroadcastNewCallToAuthoritiesAsync(CallSession session)
    {
        await _callHub.Clients.Group(CallHub.AuthoritiesGroup).SendAsync("NewCallStarted", new
        {
            CallId = session.CallId,
            RemoteNumber = session.RemoteNumber,
            Direction = session.Direction.ToString(),
            session.TriggeredByUserId,
            Timestamp = session.CreatedAt
        });
    }

    private async Task BroadcastCallEndedAsync(CallSession session)
    {
        var callGroup = $"call_{session.CallId}";
        var duration = session.EndedAt.HasValue && session.ConnectedAt.HasValue
            ? (int)(session.EndedAt - session.ConnectedAt).Value.TotalSeconds
            : (int?)null;

        await _callHub.Clients.Group(callGroup).SendAsync("CallEnded", new
        {
            CallId = session.CallId,
            Reason = "Mock call completed",
            DurationSeconds = duration,
            TranscriptSummary = await GetFullTranscriptAsync(session.CallId)
        });
    }
}
```

Note: Missing `using Microsoft.EntityFrameworkCore;` at top. Also fix `_dbContext.AICallLogs.FirstOrDefaultAsync` needs the namespace.

**Step 2: Fix using statements**

Add at top:
```csharp
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
```

**Step 3: Build verification**

---

### Task 8: Register Services in Program.cs

**Files:** Modify: `SafeZone.Server/Program.cs`

**Current registrations (around line 127):**
```csharp
builder.Services.AddSingleton<SafeZone.Server.Services.ISpeechToText, SafeZone.Server.Services.MockSttService>();
builder.Services.AddSingleton<SafeZone.Server.Services.ILanguageModel, SafeZone.Server.Services.MockLlmService>();
builder.Services.AddSingleton<SafeZone.Server.Services.ITextToSpeech, SafeZone.Server.Services.MockTtsService>();
```

**Step 1: Add after existing voice registrations**

Add these lines BEFORE `builder.Services.AddCors`:

```csharp
builder.Services.AddSingleton<SafeZone.Server.Services.IVoicePipeline, SafeZone.Server.Services.VoicePipelineService>();
builder.Services.AddSingleton<SafeZone.Server.Services.IVoiceCallService, SafeZone.Server.Services.VoiceCallService>();
```

**Step 2: Add CallHub mapping after existing hubs**

Find existing hub mappings (around line ~145-155):
```csharp
app.MapHub<SafeZone.Server.Hubs.IncidentHub>("/hubs/incidents");
app.MapHub<SafeZone.Server.Hubs.AlertHub>("/hubs/alerts");
app.MapHub<SafeZone.Server.Hubs.MapHub>("/hubs/map");
```

Add after:
```csharp
app.MapHub<SafeZone.Server.Hubs.CallHub>("/hubs/calls");
```

**Step 3: Build verification**

---

### Task 9: Update SosService to Trigger VoiceCallService

**Files:** Modify: `SafeZone.Server/Services/SosService.cs`

**Step 1: Add using statements and inject IVoiceCallService**

Add using:
```csharp
using SafeZone.Server.Hubs;
```

Modify constructor:
```csharp
private readonly SafeZoneDbContext _context;
private readonly IConfiguration _config;
private readonly IVoiceCallService _voiceCallService;

public SosService(
    SafeZoneDbContext context, 
    IConfiguration config,
    IVoiceCallService voiceCallService)
{
    _context = context;
    _config = config;
    _voiceCallService = voiceCallService;
}
```

**Step 2: Update TriggerEmergencyAsync to start voice call**

Find the end of `TriggerEmergencyAsync` where it returns:

```csharp
return new SosResponseDto
{
    CallLogId = callLog.LogId,
    ...
};
```

**Before** the return, add:

```csharp
if (IsMockMode)
{
    var emergencyPrompt = GenerateEmergencyPrompt(dto.EmergencyType, dto.Latitude, dto.Longitude);
    
    _ = Task.Run(async () =>
    {
        try
        {
            var callSession = await _voiceCallService.StartOutboundCallAsync(
                emergencyNumber,
                emergencyPrompt,
                userId);
            
            callLog.LogId = callSession.CallId;
            callLog.Status = callSession.Status;
            await _context.SaveChangesAsync();
        }
        catch
        {
        }
    });
}
```

**Step 3: Add GenerateEmergencyPrompt helper method**

Add a shorter version of the existing script generator for LLM prompt:

```csharp
private string GenerateEmergencyPrompt(AuthorityType emergencyType, double lat, double lng)
{
    var typeName = emergencyType switch
    {
        AuthorityType.Police => "Police Emergency",
        AuthorityType.Ambulance => "Medical Emergency",
        AuthorityType.FireBrigade => "Fire Emergency",
        AuthorityType.TrafficPolice => "Traffic Emergency",
        _ => "Emergency"
    };

    return $"You are the SafeZone AI Emergency Assistant. Calling {typeName} services. " +
           $"Emergency location: coordinates ({lat:F6}, {lng:F6}). " +
           $"Be calm, professional, and gather critical info: number of people, hazards, medical conditions. " +
           $"Keep responses concise.";
}
```

**Step 4: Build verification**

**Important:** Since `IVoiceCallService` is registered as Singleton but `SosService` is Scoped, you'll need to verify DI is OK. Actually, Singleton can be injected into Scoped - that's fine. Scoped into Singleton would be the problem.

---

### Task 10: Final Build and Verification

**Step 1: Full build**

```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: 0 errors, 0 warnings (or only pre-existing warnings)

**Step 2: Verify files created**

New files should exist:
- `DTOs/VoiceCallDtos.cs`
- `Services/CallSession.cs`
- `Services/IVoicePipeline.cs`
- `Services/IVoiceCallService.cs`
- `Services/VoicePipelineService.cs`
- `Services/VoiceCallService.cs`
- `Hubs/CallHub.cs`

Modified:
- `Program.cs` (DI + hub)
- `Services/SosService.cs` (voice call integration)

---

## Phase 2 Verification Summary

After all tasks:

1. **Build**: `dotnet build` - 0 errors
2. **DI**: All services registered
3. **Mock Call Flow**:
   - SOS trigger → `VoiceCallService.StartOutboundCallAsync()`
   - Mock loop runs: Ringing → Connected → 3 mock turns → Completed
   - SignalR events broadcast to `CallHub`
4. **No migration needed**: Uses existing patterns

---

## Next: Phase 3

Real providers: Whisper STT, Groq LLM, Piper TTS
