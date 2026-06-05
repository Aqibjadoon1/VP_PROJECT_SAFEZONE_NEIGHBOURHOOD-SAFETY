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
        _logger.LogInformation("Started emergency call simulation: CallId={CallId}, Number={PhoneNumber}", callId, phoneNumber);

        _ = RunCallLoopAsync(session, cancellationToken);

        await BroadcastCallStatusAsync(session);
        await BroadcastNewCallToAuthoritiesAsync(session);

        return session;
    }

    private static string GetDefaultEmergencyPrompt()
    {
        return "You are the SafeZone AI Emergency Assistant. Be professional, calm, and clear. Gather critical information: location, people involved, hazards, medical conditions.";
    }

    private async Task RunCallLoopAsync(CallSession session, CancellationToken ct)
    {
        try
        {
            await Task.Delay(800, ct);
            session.Status = CallStatus.Ringing;
            await BroadcastCallStatusAsync(session);

            await Task.Delay(1200, ct);
            session.Status = CallStatus.Answered;
            session.ConnectedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);

            var opening = "Hello, this is the SafeZone Emergency Assistant. We have a report of an emergency at your location.";
            session.Transcript.Add(new() { Speaker = SpeakerRole.Agent, Text = opening, Timestamp = DateTime.UtcNow });
            session.ConversationHistory.Add(new(ChatRole.Assistant, opening));
            await BroadcastTranscriptAsync(session.CallId, SpeakerRole.Agent, opening);

            var inputs = new[]
            {
                "Yes, this is an emergency. There's been an accident.",
                "Two people are involved. One is unconscious.",
                "The location is near the main intersection."
            };

            foreach (var input in inputs)
            {
                await Task.Delay(1500, ct);
                session.Transcript.Add(new() { Speaker = SpeakerRole.User, Text = input, Timestamp = DateTime.UtcNow });
                session.ConversationHistory.Add(new(ChatRole.User, input));
                await BroadcastTranscriptAsync(session.CallId, SpeakerRole.User, input);

                var response = await _pipeline.ProcessTurnAsync(
                    GenerateSilentAudio(16000, 1.5),
                    session.ConversationHistory,
                    session.SystemPrompt, ct);

                if (!string.IsNullOrWhiteSpace(response))
                {
                    session.Transcript.Add(new() { Speaker = SpeakerRole.Agent, Text = response, Timestamp = DateTime.UtcNow });
                    session.ConversationHistory.Add(new(ChatRole.Assistant, response));
                    await BroadcastTranscriptAsync(session.CallId, SpeakerRole.Agent, response);
                }
            }

            await Task.Delay(1000, ct);
            session.Status = CallStatus.Completed;
            session.EndedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);
            await UpdateAICallLogAsync(session);
            _logger.LogInformation("Call completed: CallId={CallId}", session.CallId);
        }
        catch (OperationCanceledException)
        {
            session.Status = CallStatus.Cancelled;
            session.EndedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Call failed: CallId={CallId}", session.CallId);
            session.Status = CallStatus.Failed;
            session.EndedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(session);
        }
        finally
        {
            _activeCalls.TryRemove(session.CallId, out _);
        }
    }

    private static byte[] GenerateSilentAudio(int sampleRate, double seconds)
    {
        var count = (int)(sampleRate * seconds);
        return new byte[count * 2];
    }

    public Task<CallSession?> GetCallAsync(Guid callId)
    {
        _activeCalls.TryGetValue(callId, out var s);
        return Task.FromResult(s);
    }

    public Task<List<CallSession>> GetActiveCallsAsync()
    {
        return Task.FromResult(_activeCalls.Values.ToList());
    }

    public async Task EndCallAsync(Guid callId, string? reason = null)
    {
        if (_activeCalls.TryRemove(callId, out var s))
        {
            s.Status = CallStatus.Completed;
            s.EndedAt = DateTime.UtcNow;
            await BroadcastCallStatusAsync(s);
        }
    }

    public Task<string?> GetFullTranscriptAsync(Guid callId)
    {
        if (_activeCalls.TryGetValue(callId, out var s))
        {
            var t = string.Join("\n", s.Transcript.Select(x => $"[{x.Speaker}] {x.Text}"));
            return Task.FromResult<string?>(t);
        }
        return Task.FromResult<string?>(null);
    }

    private async Task UpdateAICallLogAsync(CallSession session)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeZoneDbContext>();
        var log = await db.AICallLogs
            .OrderByDescending(l => l.InitiatedAt)
            .FirstOrDefaultAsync(l => l.TriggeredByUserId == session.TriggeredByUserId);
        if (log != null && session.EndedAt.HasValue && session.ConnectedAt.HasValue)
        {
            log.DurationSeconds = (int)(session.EndedAt - session.ConnectedAt).Value.TotalSeconds;
            await db.SaveChangesAsync();
        }
    }

    private async Task BroadcastCallStatusAsync(CallSession session) =>
        await _callHub.Clients.Group($"call_{session.CallId}").SendAsync("CallStatusUpdated", new
        {
            CallId = session.CallId, Status = session.Status.ToString(),
            session.RemoteNumber, Direction = session.Direction.ToString(),
            session.CreatedAt, session.ConnectedAt, session.EndedAt
        });

    private async Task BroadcastTranscriptAsync(Guid callId, SpeakerRole speaker, string text) =>
        await _callHub.Clients.Group($"call_{callId}").SendAsync("TranscriptSegment", new
        {
            CallId = callId, Speaker = speaker.ToString(), Text = text, Timestamp = DateTime.UtcNow
        });

    private async Task BroadcastNewCallToAuthoritiesAsync(CallSession session) =>
        await _callHub.Clients.Group(CallHub.AuthoritiesGroup).SendAsync("NewCallStarted", new
        {
            CallId = session.CallId, session.RemoteNumber, Direction = session.Direction.ToString(),
            session.TriggeredByUserId, Timestamp = session.CreatedAt
        });
}
