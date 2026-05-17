using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SafeZone.Server.Data;
using SafeZone.Server.Hubs;
using SafeZone.Server.Models;
using System.Collections.Concurrent;

namespace SafeZone.Server.Services;

public class VoiceCallService : IVoiceCallService
{
    private readonly ConcurrentDictionary<Guid, CallSession> _activeCalls = new();
    private readonly IVoicePipeline _pipeline;
    private readonly IHubContext<CallHub> _callHub;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<VoiceCallService> _logger;

    public bool IsMockMode => true;

    public VoiceCallService(
        IVoicePipeline pipeline,
        IHubContext<CallHub> callHub,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<VoiceCallService> logger)
    {
        _pipeline = pipeline;
        _callHub = callHub;
        _serviceScopeFactory = serviceScopeFactory;
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
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SafeZoneDbContext>();

        var log = await dbContext.AICallLogs
            .OrderByDescending(l => l.InitiatedAt)
            .FirstOrDefaultAsync(l => 
                (session.TriggeredByUserId == null || l.TriggeredByUserId == session.TriggeredByUserId) &&
                l.Status == CallStatus.Completed);

        if (log != null)
        {
            if (session.EndedAt.HasValue && session.ConnectedAt.HasValue)
            {
                log.DurationSeconds = (int)(session.EndedAt - session.ConnectedAt).Value.TotalSeconds;
            }
            await dbContext.SaveChangesAsync();
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
