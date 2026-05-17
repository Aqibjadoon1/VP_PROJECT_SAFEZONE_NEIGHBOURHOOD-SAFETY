# Design Specification: AI Calling Agent - Phase 2

**Date:** 2026-05-11  
**Status:** Draft  
**Depends On:** Phase 1 (Interfaces + Mocks) Complete

---

## Overview

Phase 2 implements the voice orchestration layer that sits between the provider interfaces (Phase 1) and the actual call control (Phase 7, SIP/FreeSWITCH).

**New Components:**
1. `VoicePipelineService` - Orchestrates STT → LLM → TTS loop
2. `CallSession` - In-memory call state tracking
3. `CallHub` - SignalR hub for real-time UI updates
4. `VoiceCallService` - High-level API for SOS integration
5. `IVoicePipeline` / `IVoiceCallService` - Interfaces

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      SOS / External Caller                        │
│  (SosController, future endpoints)                               │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          v
┌─────────────────────────────────────────────────────────────────┐
│                    VoiceCallService (High-Level)                  │
│  - StartCall(phoneNumber, context)                               │
│  - EndCall(callId)                                                │
│  - GetCallStatus(callId)                                          │
│  - Manages CallSession dictionary                                 │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          v
┌─────────────────────────────────────────────────────────────────┐
│                    VoicePipelineService (Loop)                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  While call active:                                       │   │
│  │    1. Listen (VAD detect speech)                          │   │
│  │    2. STT: audio → text transcript                         │   │
│  │    3. LLM: transcript + history → response text           │   │
│  │    4. TTS: response text → audio bytes                     │   │
│  │    5. Play audio + SignalR broadcast to UI                 │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          v
┌─────────────────────────────────────────────────────────────────┐
│                    Providers (Phase 1)                            │
│  ISpeechToText  ILanguageModel  ITextToSpeech  IVoiceActivity   │
│  (MockStt)      (MockLlm)       (MockTts)      (not used yet)   │
└─────────────────────────────────────────────────────────────────┘
                          │
                          v
┌─────────────────────────────────────────────────────────────────┐
│                    SignalR CallHub (NEW)                         │
│  Events broadcast to UI:                                          │
│  - CallStatusUpdated (Ringing → Connected → Completed)           │
│  - TranscriptSegment (STT result, speaker: User/Agent)           │
│  - AgentSpeakingStarted / AgentSpeakingStopped                    │
│  - CallEnded (summary)                                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## New Models

### CallSession (In-Memory)

Not persisted to DB directly - lightweight tracking for active calls.

```csharp
public record CallSession
{
    public Guid CallId { get; init; }
    public string RemoteNumber { get; init; }
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
    public double StartSeconds { get; init; }
    public double? EndSeconds { get; set; }
}

public enum SpeakerRole
{
    User,
    Agent,
    System
}
```

Note: `CallStatus` already exists in `Models/Enums.cs`:
- `Initiated`, `Ringing`, `Answered`, `Completed`, `Failed`, `NoAnswer`, `Cancelled`

---

## New Interfaces

### IVoicePipeline

```csharp
public interface IVoicePipeline : IDisposable
{
    Task<string> ProcessTurnAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    Task<(string transcript, byte[] responseAudio)> ProcessTurnWithAudioAsync(
        byte[] userAudio,
        List<ChatMessage> conversationHistory,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> SynthesizeTextAsync(string text, CancellationToken cancellationToken = default);

    Task<string> TranscribeAudioAsync(byte[] audio, CancellationToken cancellationToken = default);
}
```

### IVoiceCallService

```csharp
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

    Task<string?> GetCurrentTranscriptAsync(Guid callId);

    bool IsMockMode { get; }
}
```

---

## SignalR CallHub

**Route:** `/hubs/calls`

**Server Methods (Client → Server):**

```csharp
// Join for real-time updates (authorities only)
Task JoinCallMonitoring();
Task LeaveCallMonitoring();

// Join specific call updates
Task JoinCallUpdates(Guid callId);
Task LeaveCallUpdates(Guid callId);
```

**Client Events (Server → Client):**

```csharp
// Call status changes
Clients.All.SendAsync("CallStatusUpdated", new {
    CallId = Guid,
    Status = string,  // "Ringing", "Connected", "Completed", "Failed"
    RemoteNumber = string,
    Direction = string,  // "Outbound", "Inbound"
    Timestamp = DateTime
});

// New transcript segment
Clients.All.SendAsync("TranscriptSegment", new {
    CallId = Guid,
    Speaker = string,  // "User", "Agent"
    Text = string,
    Timestamp = DateTime
});

// Agent speaking state
Clients.All.SendAsync("AgentSpeaking", new {
    CallId = Guid,
    IsSpeaking = bool
});

// Call ended
Clients.All.SendAsync("CallEnded", new {
    CallId = Guid,
    Reason = string?,
    DurationSeconds = int?,
    TranscriptSummary = string
});

// New call started (for authorities)
Clients.Group("call-monitors").SendAsync("NewCallStarted", new {
    CallId = Guid,
    RemoteNumber = string,
    Direction = string,
    TriggeredByUserId = Guid?,
    Timestamp = DateTime
});
```

**Authorization:**
- `[Authorize]` on hub class
- `"authorities"` role for `JoinCallMonitoring()`

---

## Mock Call Flow (Phase 2)

Since Phase 7 (SIP) is later, Phase 2 uses **mock audio** to test the pipeline:

```
SosService.TriggerEmergencyAsync()
    │
    ├─── Creates Incident + AICallLog (existing behavior)
    │
    └─── Calls VoiceCallService.StartOutboundCallAsync()
              │
              ├─── Creates CallSession (in-memory)
              ├─── Broadcasts "CallStatusUpdated" (Initiated → Ringing)
              │
              └─── Starts mock VoicePipeline loop:
                    │
                    ├─── Wait 1-2 seconds (mock "ringing")
                    ├─── Broadcast "CallStatusUpdated" (Answered/Connected)
                    │
                    └─── Loop 2-3 times:
                          ├─── Generate mock user audio/transcript
                          ├─── Add to Transcript
                          ├─── Broadcast "TranscriptSegment" (User)
                          ├─── LLM: generate response
                          ├─── Add to ConversationHistory
                          ├─── Broadcast "TranscriptSegment" (Agent)
                          ├─── Broadcast "AgentSpeaking" (true → false)
                          ├─── Wait 500ms
                    │
                    └─── End call:
                          ├─── Broadcast "CallStatusUpdated" (Completed)
                          ├─── Broadcast "CallEnded"
                          ├─── Update AICallLog in DB (Transcript, Duration)
```

**No actual audio hardware needed** - mock pipeline works entirely in memory.

---

## Changes to Existing Files

### SosService.cs

**Modified:** After creating Incident + AICallLog, also:
1. Inject `IVoiceCallService` via constructor
2. Call `StartOutboundCallAsync()` with emergency number
3. Store `CallSession.CallId` in `AICallLog` (new field? or use existing `LogId`)

**Option A**: Reuse `AICallLog.LogId` as `CallSession.CallId` (Guid match)

**Option B**: Add `AICallLog.CallSessionId` field (migration needed)

**Recommended**: Option A - simpler, no migration needed.

### Program.cs

**New DI Registrations:**
```csharp
builder.Services.AddSingleton<IVoicePipeline, VoicePipelineService>();
builder.Services.AddSingleton<IVoiceCallService, VoiceCallService>();

// Hub mapping after existing hubs:
app.MapHub<SafeZone.Server.Hubs.CallHub>("/hubs/calls");
```

---

## New DTOs

### VoiceCallDtos.cs

```csharp
namespace SafeZone.Server.DTOs;

public record StartCallDto
{
    [Required]
    public string PhoneNumber { get; init; }
    public string? SystemPrompt { get; init; }
    public AuthorityType? EmergencyType { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public record CallResponseDto
{
    public Guid CallId { get; init; }
    public string RemoteNumber { get; init; }
    public string Status { get; init; }
    public string Direction { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsMockMode { get; init; }
}

public record CallStatusDto
{
    public Guid CallId { get; init; }
    public string Status { get; init; }
    public DateTime? ConnectedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int? DurationSeconds { get; init; }
    public List<TranscriptSegmentDto>? Transcript { get; init; }
}

public record TranscriptSegmentDto
{
    public string Speaker { get; init; }
    public string Text { get; init; }
    public DateTime Timestamp { get; init; }
}
```

---

## Files to Create

| File | Description |
|------|-------------|
| `Services/IVoicePipeline.cs` | Pipeline interface |
| `Services/IVoiceCallService.cs` | Call service interface |
| `Services/VoicePipelineService.cs` | STT→LLM→TTS loop orchestration |
| `Services/VoiceCallService.cs` | Call session management |
| `Hubs/CallHub.cs` | SignalR hub for real-time updates |
| `DTOs/VoiceCallDtos.cs` | DTOs for API |
| `Models/TranscriptSegment.cs` | Transcript model (or in DTOs) |

---

## Files to Modify

| File | Change |
|------|--------|
| `Services/SosService.cs` | Inject `IVoiceCallService`, start mock pipeline |
| `Services/ISosService.cs` | No changes needed (interface unchanged) |
| `Program.cs` | Add DI registrations + hub mapping |

---

## Test Plan

Since this is mock-only:

1. **Build**: `dotnet build` - 0 errors
2. **DI Verify**: Services injectable
3. **Mock Call**: Programmatically verify:
   - `StartOutboundCallAsync()` creates CallSession
   - Pipeline loop generates mock transcripts
   - SignalR events fire (if we can test)
4. **SosService Integration**: Existing SOS trigger now also creates mock voice call

---

## Next Phases After This

- **Phase 3**: Real providers (Whisper STT, Groq LLM, Piper TTS)
- **Phase 4**: VoiceCallController API endpoints (if needed beyond SOS)
- **Phase 5**: SMS Service
- **Phase 6**: Inbound call handling
- **Phase 7**: SIP + FreeSWITCH for actual phone calls
- **Phase 8**: AI Calling Agent Controller + UI integration

---

## Decisions Record

1. **Mock-first**: Phase 2 uses mock audio loop, no SIP hardware needed
2. **CallSession in-memory**: No DB persistence for active calls (simpler)
3. **AICallLog for persistence**: Existing `AICallLogs` table stores completed call data
4. **SignalR for UI**: Existing `AlertHub` pattern reused for `CallHub`
5. **No migration**: Reuse `AICallLog.LogId` as `CallSession.CallId`

---

**End of Phase 2 Specification**
