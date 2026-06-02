# SafeZone Voice AI Pipeline

## Architecture Overview

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  STT          │ -> │  LLM          │ -> │  TTS          │
│  Speech→Text  │    │  Text→Response│    │  Response→Audio│
└──────────────┘    └──────────────┘    └──────────────┘
       |                  |                  |
       └──────────────────┼──────────────────┘
                          |
                 VoicePipelineService
                 (orchestrator)
                          |
                 VoiceCallService
                 (session management)
```

## Services

| Interface | Implementation | Description |
|-----------|---------------|-------------|
| `ISpeechToText` | `MockSttService` | Returns curated emergency transcripts |
| `ILanguageModel` | `GroqLlmService` | Groq API with mock fallback |
| `ITextToSpeech` | `MockTtsService` | Generates WAV audio |
| `IVoicePipeline` | `VoicePipelineService` | STT→LLM→TTS orchestrator |
| `IVoiceCallService` | `VoiceCallService` | Call session management + SignalR broadcast |

## Real-Time Events (CallHub)

| Event | Description |
|-------|-------------|
| `CallStatusUpdated` | Call state changes |
| `TranscriptSegment` | Live transcript segments |
| `AgentSpeaking` | Agent speech indicator |
| `NewCallStarted` | New inbound/outbound call |
| `CallEnded` | Call completion |

## Configuration

All API keys are optional — services gracefully fall back to mock implementations when keys are not configured.
