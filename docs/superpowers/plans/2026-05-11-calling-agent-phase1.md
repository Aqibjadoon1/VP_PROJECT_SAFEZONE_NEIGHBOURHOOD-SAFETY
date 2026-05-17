# AI Calling Agent - Phase 1: Core Voice Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the core voice pipeline interfaces and mock providers. This establishes the abstraction layer that will later support real Whisper STT, Groq LLM, and Piper TTS.

**Architecture:** Provider interfaces (ISpeechToText, ILanguageModel, ITextToSpeech) with mock implementations that return predictable, fixed responses. All mocks return `IsMock = true`.

**Tech Stack:** C# .NET 8.0, ASP.NET Core, xUnit (if tests added)

---

## File Structure

| File | Responsibility |
|------|----------------|
| `Services/ISpeechToText.cs` | STT provider interface |
| `Services/ILanguageModel.cs` | LLM provider interface + ChatMessage model |
| `Services/ITextToSpeech.cs` | TTS provider interface |
| `Services/IVoiceActivityDetector.cs` | VAD interface |
| `Services/MockSttService.cs` | Mock STT implementation |
| `Services/MockLlmService.cs` | Mock LLM implementation |
| `Services/MockTtsService.cs` | Mock TTS implementation |
| `Program.cs` | DI registration |

---

### Task 1: Create ChatMessage Model and ILanguageModel Interface

**Files:**
- Create: `SafeZone.Server/Services/ILanguageModel.cs`

- [ ] **Step 1: Create the interface file**

```csharp
namespace SafeZone.Server.Services;

public record ChatMessage
{
    public ChatRole Role { get; init; }
    public string Content { get; init; } = string.Empty;

    public ChatMessage() { }

    public ChatMessage(ChatRole role, string content)
    {
        Role = role;
        Content = content;
    }
}

public enum ChatRole
{
    System,
    User,
    Assistant
}

public interface ILanguageModel : IDisposable
{
    Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    bool IsMock { get; }
}
```

- [ ] **Step 2: Run build to verify it compiles**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds with this new file included.

---

### Task 2: Create ISpeechToText Interface

**Files:**
- Create: `SafeZone.Server/Services/ISpeechToText.cs`

- [ ] **Step 1: Create the interface file**

```csharp
namespace SafeZone.Server.Services;

public interface ISpeechToText : IDisposable
{
    Task<string> TranscribeAsync(byte[] audioData, int sampleRate = 16000);

    IAsyncEnumerable<string> TranscribeStreamAsync(
        Stream audioStream,
        int sampleRate = 16000,
        CancellationToken cancellationToken = default);

    bool IsMock { get; }
}
```

- [ ] **Step 2: Run build to verify**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds.

---

### Task 3: Create ITextToSpeech Interface

**Files:**
- Create: `SafeZone.Server/Services/ITextToSpeech.cs`

- [ ] **Step 1: Create the interface file**

```csharp
namespace SafeZone.Server.Services;

public interface ITextToSpeech : IDisposable
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default);

    int SampleRate { get; }
    int Channels { get; }
    bool IsMock { get; }
}
```

- [ ] **Step 2: Run build to verify**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds.

---

### Task 4: Create IVoiceActivityDetector Interface

**Files:**
- Create: `SafeZone.Server/Services/IVoiceActivityDetector.cs`

- [ ] **Step 1: Create the interface file**

```csharp
namespace SafeZone.Server.Services;

public interface IVoiceActivityDetector : IDisposable
{
    bool IsSpeech(byte[] audioData, int sampleRate = 16000);

    float LastSpeechProbability { get; }
}
```

- [ ] **Step 2: Run build to verify**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds.

---

### Task 5: Create MockLlmService Implementation

**Files:**
- Create: `SafeZone.Server/Services/MockLlmService.cs`

- [ ] **Step 1: Create mock LLM service**

```csharp
namespace SafeZone.Server.Services;

public class MockLlmService : ILanguageModel
{
    private readonly Dictionary<ChatRole, string> _rolePrefixes = new()
    {
        { ChatRole.System, "[System] " },
        { ChatRole.User, "[User] " },
        { ChatRole.Assistant, "[Assistant] " }
    };

    private readonly string _emergencyResponse =
        "This is the SafeZone AI emergency assistant. I understand there's an emergency situation. " +
        "Please stay on the line. Emergency services have been notified of your location. " +
        "Can you tell me how many people are involved? Are there any immediate hazards I should know about?";

    private readonly string _defaultResponse =
        "Thank you for your message. This is a mock AI response. " +
        "In a real implementation, this would connect to an LLM provider like Groq or OpenAI.";

    public bool IsMock => true;

    public Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessage>? conversationHistory = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return Task.FromResult(_defaultResponse);
        }

        var lowerMessage = userMessage.ToLowerInvariant();

        if (lowerMessage.Contains("emergency") ||
            lowerMessage.Contains("help") ||
            lowerMessage.Contains("sos") ||
            lowerMessage.Contains("fire") ||
            lowerMessage.Contains("police") ||
            lowerMessage.Contains("ambulance") ||
            lowerMessage.Contains("accident") ||
            lowerMessage.Contains("hurt") ||
            lowerMessage.Contains("injured"))
        {
            return Task.FromResult(_emergencyResponse);
        }

        if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi"))
        {
            return Task.FromResult("Hello! This is the SafeZone AI assistant. How can I help you today?");
        }

        if (lowerMessage.Contains("location"))
        {
            return Task.FromResult("I understand you're providing location information. I've noted your coordinates. Emergency services are being dispatched to your location. Please remain calm and stay on the line if possible.");
        }

        if (lowerMessage.Contains("yes") || lowerMessage.Contains("ok") || lowerMessage.Contains("okay"))
        {
            return Task.FromResult("Understood. Is there anything else you need to report? Any additional details would help emergency responders.");
        }

        if (lowerMessage.Contains("no"))
        {
            return Task.FromResult("Alright. Please stay safe. Emergency services are on their way. If the situation changes, please let me know immediately.");
        }

        return Task.FromResult(_defaultResponse);
    }

    public void Dispose()
    {
    }
}
```

- [ ] **Step 2: Run build to verify**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds.

---

### Task 6: Create MockSttService Implementation

**Files:**
- Create: `SafeZone.Server/Services/MockSttService.cs`

- [ ] **Step 1: Create mock STT service**

```csharp
namespace SafeZone.Server.Services;

public class MockSttService : ISpeechToText
{
    private static readonly string[] _mockTranscripts = new[]
    {
        "Help, there's an emergency!",
        "There's a fire at my location.",
        "Someone is injured, please send an ambulance.",
        "I see suspicious activity, need police.",
        "There's been an accident.",
        "Please help, I'm in danger.",
        "My location is critical.",
        "Emergency services needed immediately."
    };

    private int _transcriptIndex = 0;
    private readonly object _lock = new();

    public bool IsMock => true;

    public Task<string> TranscribeAsync(byte[] audioData, int sampleRate = 16000)
    {
        if (audioData == null || audioData.Length == 0)
        {
            return Task.FromResult(string.Empty);
        }

        lock (_lock)
        {
            var transcript = _mockTranscripts[_transcriptIndex % _mockTranscripts.Length];
            _transcriptIndex++;
            return Task.FromResult(transcript);
        }
    }

    public async IAsyncEnumerable<string> TranscribeStreamAsync(
        Stream audioStream,
        int sampleRate = 16000,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await audioStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            if (bytesRead > 100)
            {
                lock (_lock)
                {
                    var transcript = _mockTranscripts[_transcriptIndex % _mockTranscripts.Length];
                    _transcriptIndex++;
                    yield return transcript;
                }
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    public void Dispose()
    {
    }
}
```

- [ ] **Step 2: Run build to verify**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds.

---

### Task 7: Create MockTtsService Implementation

**Files:**
- Create: `SafeZone.Server/Services/MockTtsService.cs`

- [ ] **Step 1: Create mock TTS service**

```csharp
namespace SafeZone.Server.Services;

public class MockTtsService : ITextToSpeech
{
    public int SampleRate => 16000;
    public int Channels => 1;
    public bool IsMock => true;

    private static readonly byte[] _silentWavHeader = new byte[]
    {
        0x52, 0x49, 0x46, 0x46,
        0x00, 0x00, 0x00, 0x00,
        0x57, 0x41, 0x56, 0x45,
        0x66, 0x6D, 0x74, 0x20,
        0x10, 0x00, 0x00, 0x00,
        0x01, 0x00,
        0x01, 0x00,
        0x80, 0x3E, 0x00, 0x00,
        0x00, 0x7D, 0x00, 0x00,
        0x02, 0x00,
        0x10, 0x00,
        0x64, 0x61, 0x74, 0x61,
        0x00, 0x00, 0x00, 0x00
    };

    public Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        var durationMs = Math.Max(500, text.Length * 60);
        var sampleCount = (int)(SampleRate * durationMs / 1000.0);
        var byteCount = sampleCount * 2;

        var wavData = new byte[_silentWavHeader.Length + byteCount];
        _silentWavHeader.CopyTo(wavData, 0);

        var fileSize = wavData.Length - 8;
        wavData[4] = (byte)(fileSize & 0xFF);
        wavData[5] = (byte)((fileSize >> 8) & 0xFF);
        wavData[6] = (byte)((fileSize >> 16) & 0xFF);
        wavData[7] = (byte)((fileSize >> 24) & 0xFF);

        var dataSize = byteCount;
        wavData[40] = (byte)(dataSize & 0xFF);
        wavData[41] = (byte)((dataSize >> 8) & 0xFF);
        wavData[42] = (byte)((dataSize >> 16) & 0xFF);
        wavData[43] = (byte)((dataSize >> 24) & 0xFF);

        var random = new Random(text.GetHashCode());
        for (int i = 0; i < sampleCount; i++)
        {
            var sample = (short)(random.NextDouble() * 50 - 25);
            var offset = _silentWavHeader.Length + i * 2;
            wavData[offset] = (byte)(sample & 0xFF);
            wavData[offset + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return Task.FromResult(wavData);
    }

    public void Dispose()
    {
    }
}
```

- [ ] **Step 2: Run build to verify**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds.

---

### Task 8: Register Services in Program.cs DI

**Files:**
- Modify: `SafeZone.Server/Program.cs` (around line 119-125 where existing services are registered)

- [ ] **Step 1: Read existing service registrations to understand the pattern**

First check existing pattern. Expected existing registrations (around line 119):
```csharp
builder.Services.AddScoped<SafeZone.Server.Services.IAuthService, SafeZone.Server.Services.AuthService>();
builder.Services.AddScoped<SafeZone.Server.Services.IIncidentService, SafeZone.Server.Services.IncidentService>();
builder.Services.AddScoped<SafeZone.Server.Services.IAlertService, SafeZone.Server.Services.AlertService>();
builder.Services.AddScoped<SafeZone.Server.Services.IFirService, SafeZone.Server.Services.FirService>();
builder.Services.AddScoped<SafeZone.Server.Services.ISosService, SafeZone.Server.Services.SosService>();
```

- [ ] **Step 2: Add voice pipeline service registrations AFTER existing SosService registration**

Add this block right after the existing `ISosService` registration:

```csharp
builder.Services.AddSingleton<SafeZone.Server.Services.ISpeechToText, SafeZone.Server.Services.MockSttService>();
builder.Services.AddSingleton<SafeZone.Server.Services.ILanguageModel, SafeZone.Server.Services.MockLlmService>();
builder.Services.AddSingleton<SafeZone.Server.Services.ITextToSpeech, SafeZone.Server.Services.MockTtsService>();
```

Note: Use `AddSingleton` because these are stateless providers that don't need per-request scope.

- [ ] **Step 3: Run full build to verify everything compiles together**

Run:
```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

Expected: Build succeeds with 0 errors.

---

## Phase 1 Verification

After completing all tasks, run this verification:

```bash
cd "C:\Users\jadoo\Desktop\coal proj\VISUAL PROGRAMMING PROJECT\SafeZone.Server"
dotnet build
```

**Expected:**
- Build: `0 Error(s)`
- All 8 files created:
  - `Services/ILanguageModel.cs`
  - `Services/ISpeechToText.cs`
  - `Services/ITextToSpeech.cs`
  - `Services/IVoiceActivityDetector.cs`
  - `Services/MockLlmService.cs`
  - `Services/MockSttService.cs`
  - `Services/MockTtsService.cs`
  - `Program.cs` (modified with DI registrations)

**Functional Check (if server running):**
- Services registered in DI
- All interfaces available for injection
- Mock implementations return `IsMock = true`

---

## Next Phase

After Phase 1 is verified and working, proceed to:
- **Phase 2:** VoicePipelineService (orchestrates STT→LLM→TTS loop)
- **Phase 3:** CallSession model + VoiceCallService (orchestrator)
- **Phase 4:** SignalR CallHub for real-time updates
- **Phase 5:** Update SosService to use VoiceCallService
- **Phase 6:** Real providers (Whisper STT, Groq LLM, Piper TTS)
- **Phase 7:** SIP + FreeSWITCH integration
