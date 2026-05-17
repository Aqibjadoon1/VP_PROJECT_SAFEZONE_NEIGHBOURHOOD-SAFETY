# Design Specification: AI Calling Agent (C#/.NET Port)

**Date:** 2026-05-11  
**Status:** Draft  
**Author:** SafeZone Team

---

## Overview

Port the Dograh voice agent platform capabilities to SafeZone's native C#/.NET architecture. Integrate real-time voice AI with the existing SOS emergency system.

---

## Goals

1. **Real-time AI Conversation**: Two-way voice interaction using STT → LLM → TTS pipeline
2. **SOS Emergency Calls**: Outbound calls to emergency services with location-aware scripts
3. **SMS Notifications**: Send SMS alerts to emergency contacts
4. **Inbound Call Handling**: Answer incoming calls with AI greeting
5. **Mock Mode**: All providers work without API keys for development
6. **Zero External Cost**: Use local models and free-tier APIs

---

## Non-Goals

- Full Dograh platform (workflow builder, UI dashboard)
- WebRTC browser calling (focus on SIP/telephony first)
- Vendor lock-in to any specific cloud provider

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           SafeZone.Server                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────────────────┐ │
│  │  Frontend    │───>│   API Layer  │───>│   Core Services          │ │
│  │  (wwwroot/)  │    │  Controllers │    │                          │ │
│  └──────────────┘    └──────────────┘    │  ┌────────────────────┐  │ │
│                                            │  │ VoiceCallService    │  │ │
│                                            │  └────────────────────┘  │ │
│                                            │  ┌────────────────────┐  │ │
│                                            │  │ VoicePipeline       │  │ │
│                                            │  │ (STT→LLM→TTS loop) │  │ │
│                                            │  └────────────────────┘  │ │
│                                            │  ┌────────────────────┐  │ │
│                                            │  │ SipCallService      │  │ │
│                                            │  └────────────────────┘  │ │
│                                            │  ┌────────────────────┐  │ │
│                                            │  │ SmsService          │  │ │
│                                            │  └────────────────────┘  │ │
│                                            └──────────────────────────┘ │
│                                                         │                 │
│                                                         v                 │
│                                            ┌──────────────────────────┐ │
│                                            │   Provider Abstractions  │ │
│                                            │  ┌────────────────────┐  │ │
│                                            │  │ ISpeechToText       │  │ │
│                                            │  │   ├─ WhisperSttService│  │ │
│                                            │  │   └─ MockSttService  │  │ │
│                                            │  └────────────────────┘  │ │
│                                            │  ┌────────────────────┐  │ │
│                                            │  │ ILanguageModel       │  │ │
│                                            │  │   ├─ GroqLlmService  │  │ │
│                                            │  │   ├─ OllamaLlmService│  │ │
│                                            │  │   └─ MockLlmService  │  │ │
│                                            │  └────────────────────┘  │ │
│                                            │  ┌────────────────────┐  │ │
│                                            │  │ ITextToSpeech       │  │ │
│                                            │  │   ├─ PiperTtsService │  │ │
│                                            │  │   └─ MockTtsService  │  │ │
│                                            │  └────────────────────┘  │ │
│                                            └──────────────────────────┘ │
│                                                                           │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │                        SignalR Hubs                                 │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────┐ │ │
│  │  │ IncidentHub  │  │  CallHub     │  │      MapHub            │ │ │
│  │  │ (existing)   │  │  (NEW)       │  │      (existing)        │ │ │
│  │  └──────────────┘  └──────────────┘  └────────────────────────┘ │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                        Docker (FreeSWITCH)                               │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  FreeSWITCH PBX                                                   │   │
│  │  - SIP Registrar: extensions 1001-1010                           │   │
│  │  - Event Socket: port 8021 for control from C#                   │   │
│  │  - RTP Ports: 16384-32768 for audio                              │   │
│  │  - Default password: ClueCon (for ESL)                            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Components

### 1. Voice Pipeline (Real-Time Conversation)

**Core Service:** `VoicePipelineService`

**Pipeline Flow:**
```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌─────────┐
│  Audio   │───>│  STT     │───>│  LLM     │───>│  TTS    │
│  Input   │    │ (Local)  │    │ (Groq)   │    │ (Local) │
└──────────┘    └──────────┘    └──────────┘    └─────────┘
     ^                                              │
     │                    RTP Audio                 │
     └──────────────────── Loopback ────────────────┘
```

**Provider Interfaces:**

```csharp
// Speech-to-Text
public interface ISpeechToText : IDisposable
{
    Task<string> TranscribeAsync(byte[] audioData, int sampleRate = 16000);
    IAsyncEnumerable<string> TranscribeStreamAsync(Stream audioStream, int sampleRate = 16000);
    bool IsMock { get; }
}

// Language Model
public interface ILanguageModel : IDisposable
{
    Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
    bool IsMock { get; }
}

// Text-to-Speech
public interface ITextToSpeech : IDisposable
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
    int SampleRate { get; }
    int Channels { get; }
    bool IsMock { get; }
}

// Voice Activity Detection
public interface IVoiceActivityDetector : IDisposable
{
    bool IsSpeech(byte[] audioData, int sampleRate = 16000);
    float LastSpeechProbability { get; }
}
```

**Implementations:**

| Interface | Real Implementation | Mock Implementation |
|-----------|---------------------|---------------------|
| `ISpeechToText` | `WhisperSttService` (Whisper.net, GGML models) | `MockSttService` |
| `ILanguageModel` | `GroqLlmService` (OpenAI SDK compatible) | `MockLlmService` |
| `ILanguageModel` | `OllamaLlmService` (local fallback) | - |
| `ITextToSpeech` | `PiperTtsService` (Piper, ONNX models) | `MockTtsService` |
| `IVoiceActivityDetector` | `SileroVadService` (ONNX) | - |

### 2. SIP Call Control

**Core Service:** `SipCallService`

**Using:**
- `SIPSorcery` - .NET SIP library for call management
- `NAudio` - Audio handling and RTP processing
- FreeSWITCH ESL - Advanced call control via Event Socket

**Call Session Model (in-memory):**

```csharp
public class CallSession
{
    public string CallId { get; init; }
    public CallDirection Direction { get; init; }
    public string RemoteNumber { get; init; }
    public CallStatus Status { get; set; }
    public List<ChatMessage> ConversationHistory { get; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Transcript { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
}
```

**Supported Call Flows:**

1. **Outbound SOS Call** - Emergency services notification
2. **Outbound Notification Call** - Emergency contact notification  
3. **Inbound Call** - Answer with AI greeting and menu
4. **Call Transfer** - Transfer to human operator if needed

### 3. SMS Service

**Core Service:** `SmsService`

**Interface:**
```csharp
public interface ISmsService
{
    Task<SmsResult> SendSmsAsync(string toNumber, string message);
    Task<List<SmsResult>> SendBulkSmsAsync(List<string> toNumbers, string message);
    bool IsMock { get; }
}
```

**Implementations:**
- `MockSmsService` - Default for development
- (Future) `TwilioSmsService`, `PlivoSmsService`, `SipSmsService`

### 4. Voice Call Service (Orchestrator)

**Core Service:** `VoiceCallService`

Orchestrates the full flow:
- Creates/manages CallSession
- Starts/Stops VoicePipeline
- Updates AICallLog in database
- Broadcasts real-time updates via SignalR CallHub

**Integration with existing SosService:**
- `SosService` calls `VoiceCallService` for actual call placement
- `SosService` retains responsibility for Incident + AICallLog creation
- `IsMockMode` config controls both services

### 5. SignalR CallHub (NEW)

**Route:** `/hubs/calls`

**Client Events (Server → Client):**
- `CallStatusUpdated` - Call status change (Ringing → Connected → Completed)
- `TranscriptSegment` - New transcript chunk from STT
- `AgentSpeaking` - AI started/stopped speaking
- `CallEnded` - Call completed with summary

**Server Methods (Client → Server):**
- `JoinCallUpdates` - Subscribe to all call updates (authorities)
- `LeaveCallUpdates` - Unsubscribe

---

## Data Flow

### SOS Emergency Call Flow

```
┌──────────────┐
│  User UI     │
│  sos.html    │
└──────┬───────┘
       │
       v
┌──────────────┐
│  SosController│
│  POST /api/sos/trigger
└──────┬───────┘
       │
       v
┌──────────────┐
│  SosService  │
│  - Validate request
│  - Generate emergency script
│  - Create Incident (Critical)
│  - Create AICallLog
└──────┬───────┘
       │
       v
┌──────────────────┐
│  VoiceCallService│
│  - Create CallSession
│  - Start VoicePipeline
│  - SipCallService.PlaceCall()
└──────┬───────────┘
       │
       v
┌──────────────────┐
│  SipCallService  │
│  - Register SIP endpoint
│  - Invite to emergency number
│  - Wait for answer
│  - Start RTP audio stream
└──────┬───────────┘
       │
       v
┌──────────────────┐
│  VoicePipeline   │
│  Loop:
│    1. Capture RTP audio
│    2. VAD: detect speech
│    3. STT: transcribe to text
│    4. LLM: generate response
│    5. TTS: synthesize to audio
│    6. Send RTP audio back
│    7. SignalR: broadcast transcript
└──────────────────┘
       │
       v
┌──────────────────┐
│  Call End        │
│  - Update AICallLog:
│    Status=Completed
│    DurationSeconds
│    Transcript
│  - SignalR: CallEnded event
└──────────────────┘
```

---

## Configuration (appsettings.json)

```json
{
  "VoiceAI": {
    "UseMockMode": true,
    
    "STT": {
      "Provider": "WhisperLocal",
      "ModelPath": "models/whisper/ggml-base.bin",
      "Language": "en"
    },
    
    "LLM": {
      "Provider": "Groq",
      "ApiKey": "",
      "Endpoint": "https://api.groq.com/openai/v1",
      "ModelName": "llama-3.1-8b-instant",
      "SystemPrompt": "You are an emergency services AI assistant. You are making a call on behalf of a user in distress. Be clear, concise, and professional. Focus on gathering critical information: location, emergency type, number of people involved, any hazards. Speak calmly and clearly.",
      "MaxTokens": 500,
      "Temperature": 0.3
    },
    
    "TTS": {
      "Provider": "PiperLocal",
      "ModelPath": "models/piper/en_US-amy-medium.onnx",
      "Voice": "amy"
    }
  },
  
  "SIP": {
    "UseMockMode": true,
    "LocalPort": 5060,
    "LocalAddress": "auto",
    "SipUsername": "safezone-agent",
    "SipPassword": "",
    
    "FreeSWITCH": {
      "Host": "localhost",
      "Port": 8021,
      "Password": "ClueCon",
      "Extension": "1001"
    },
    
    "EmergencyNumbers": {
      "Police": "15",
      "Ambulance": "115",
      "Fire": "16",
      "Traffic": "1915"
    }
  },
  
  "SMS": {
    "UseMockMode": true,
    "Provider": "Mock"
  }
}
```

---

## Docker Compose (FreeSWITCH)

```yaml
services:
  freeswitch:
    image: safestream/freeswitch:latest
    container_name: freeswitch
    ports:
      - "5060:5060/udp"
      - "5060:5060/tcp"
      - "8021:8021"
      - "16384-16484:16384-16484/udp"
    volumes:
      - ./freeswitch/config:/etc/freeswitch
    environment:
      - FS_PASSWORD=ClueCon
    restart: unless-stopped
```

**Alternative**: Use official FreeSWITCH image or build custom with minimal config.

---

## Model Files (Local STT/TTS)

**Required Downloads:**

| Purpose | Model | Source | Size |
|---------|-------|--------|------|
| STT | Whisper Base (GGML) | huggingface.co/openai/whisper-base | ~140 MB |
| STT | Whisper Tiny (GGML) | huggingface.co/openai/whisper-tiny | ~75 MB |
| TTS | Piper en_US-amy | huggingface.co/rhasspy/piper-voices | ~50 MB |
| VAD | Silero VAD ONNX | github.com/snakers4/silero-vad | ~4 MB |

**Directory Structure:**
```
SafeZone.Server/
├── models/
│   ├── whisper/
│   │   ├── ggml-tiny.bin
│   │   └── ggml-base.bin
│   ├── piper/
│   │   ├── en_US-amy-medium.onnx
│   │   └── en_US-amy-medium.onnx.json
│   └── silero/
│       └── silero_vad.onnx
```

---

## NuGet Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| `SIPSorcery` | SIP protocol | Latest stable |
| `NAudio` | Audio processing | Latest stable |
| `OpenAI` | LLM API (compatible with Groq) | 2.0+ |
| `Microsoft.ML.OnnxRuntime` | ONNX for Piper/VAD | Latest |

**Whisper.NET Options:**
- `Whisper.net` (community) - OR
- Direct integration with Whisper.cpp via native bindings

**Piper Integration:**
- Custom wrapper around `piper-phonemize` and ONNX Runtime
- OR use `PiperSharp` if available

---

## Changes to Existing Files

### 1. ISosService / SosService

**Changes:**
- `VoiceCallService` injected for actual call placement
- `IsMockMode` controls whether real calls are made
- Existing Incident + AICallLog creation remains

### 2. AICallLog Model

**Possible additions:**
```csharp
// New fields (if needed)
public string? Transcript { get; set; }  // Full conversation transcript
public string? CallDirection { get; set; }  // Inbound/Outbound
public string? RecordingPath { get; set; }  // Audio recording path
```

### 3. Program.cs

**New registrations:**
```csharp
// Provider abstractions
builder.Services.AddSingleton<ISpeechToText, WhisperSttService>();
builder.Services.AddSingleton<ILanguageModel, GroqLlmService>();
builder.Services.AddSingleton<ITextToSpeech, PiperTtsService>();
builder.Services.AddSingleton<IVoiceActivityDetector, SileroVadService>();

// Core services
builder.Services.AddSingleton<SipCallService>();
builder.Services.AddSingleton<SmsService>();
builder.Services.AddScoped<VoiceCallService>();
builder.Services.AddScoped<VoicePipelineService>();

// SignalR hub mapping
app.MapHub<CallHub>("/hubs/calls");
```

---

## New Files to Create

### Services
- `Services/ISpeechToText.cs`
- `Services/ILanguageModel.cs`
- `Services/ITextToSpeech.cs`
- `Services/IVoiceActivityDetector.cs`
- `Services/WhisperSttService.cs`
- `Services/GroqLlmService.cs`
- `Services/OllamaLlmService.cs`
- `Services/PiperTtsService.cs`
- `Services/SileroVadService.cs`
- `Services/MockSttService.cs`
- `Services/MockLlmService.cs`
- `Services/MockTtsService.cs`
- `Services/SipCallService.cs`
- `Services/SmsService.cs`
- `Services/VoiceCallService.cs`
- `Services/VoicePipelineService.cs`

### Hubs
- `Hubs/CallHub.cs`

### Models
- `Models/CallSession.cs` (in-memory, not EF)
- `Models/ChatMessage.cs`

### DTOs
- `DTOs/VoiceCallDtos.cs`

---

## Migration Strategy

### Phase 1: Foundation (Now)
1. Add NuGet dependencies
2. Create interfaces + mock implementations
3. Update `SosService` to call `VoiceCallService` (mock-only)
4. Configure DI in `Program.cs`

### Phase 2: SIP + FreeSWITCH
1. Create `SipCallService` with FreeSWITCH ESL
2. Add docker-compose for FreeSWITCH
3. Test SIP registration and basic call control

### Phase 3: Voice Pipeline
1. Integrate Whisper.NET STT
2. Integrate Groq LLM (OpenAI SDK compatible)
3. Integrate Piper TTS via ONNX
4. Implement real-time loop

### Phase 4: Polish
1. SignalR CallHub for real-time UI updates
2. Transcript persistence
3. Error handling and retry logic
4. Documentation

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Whisper.NET compatibility issues | Medium | High | Fallback to mock STT first; verify NuGet package |
| Piper C# integration complexity | Medium | High | Use mock TTS first; fallback to edge TTS API |
| FreeSWITCH Docker networking | Medium | Medium | Start with mock SIP mode; add FreeSWITCH later |
| Groq rate limits in free tier | Medium | Low | Add Ollama fallback for local LLM |
| RTP audio latency | High | Medium | Use adaptive buffering; prioritize mock for development |

---

## Success Criteria

1. ✅ Mock mode works without any API keys or Docker
2. ✅ SOS trigger creates Incident + AICallLog + CallSession
3. ✅ Real-time STT → LLM → TTS loop functional (with mock providers)
4. ✅ SignalR CallHub broadcasts status updates
5. ✅ Code compiles with zero errors
6. ✅ Follows existing project patterns (records for DTOs, scoped services, etc.)

---

## Appendix: Groq Free Tier

**Groq Cloud** offers generous free tier:
- 14,400 requests per day
- 14,400,000 tokens per day
- Models: Llama 3.1 (8B, 70B), Mixtral
- API: OpenAI-compatible (use `OpenAI` SDK with custom endpoint)

**Endpoint:** `https://api.groq.com/openai/v1`  
**API Key:** Create free account at https://console.groq.com

---

## Appendix: Local LLM with Ollama

**Ollama** lets you run LLMs locally:
- Models: Llama 3.2, Mistral, Gemma
- No internet needed after download
- API: OpenAI-compatible
- Free, open source

**Installation:** https://ollama.com  
**Run model:** `ollama run llama3.2`

---

## Appendix: Emergency Numbers (Pakistan)

| Service | Number |
|---------|--------|
| Police | 15 |
| Ambulance (Edhi) | 115 |
| Fire Brigade | 16 |
| Traffic Police | 1915 |
| National Emergency | 112 |

---

**End of Specification**
