using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SafeZone.Server.Hubs;

[Authorize]
public class CallHub : Hub
{
    public const string AuthoritiesGroup = "call-monitors";
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> ConnectionGroups = new();

    public async Task JoinCallMonitoring()
    {
        if (!IsAuthorityOrHigher(Context.User))
        {
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, AuthoritiesGroup);
        TrackGroup(Context.ConnectionId, AuthoritiesGroup);
    }

    private static bool IsAuthorityOrHigher(System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user is null) return false;
        return user.IsInRole("Authority") || user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
    }

    public async Task LeaveCallMonitoring()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AuthoritiesGroup);
        UntrackGroup(Context.ConnectionId, AuthoritiesGroup);
    }

    public async Task JoinCallUpdates(Guid callId)
    {
        var groupName = $"call_{callId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        TrackGroup(Context.ConnectionId, groupName);
    }

    public async Task LeaveCallUpdates(Guid callId)
    {
        var groupName = $"call_{callId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        UntrackGroup(Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionGroups.TryRemove(Context.ConnectionId, out var groups))
        {
            foreach (var group in groups.Keys)
            {
                try { await Groups.RemoveFromGroupAsync(Context.ConnectionId, group); }
                catch (Exception)
                {
                    // Silently continue
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private static void TrackGroup(string connectionId, string groupName)
    {
        ConnectionGroups.AddOrUpdate(
            connectionId,
            _ => new ConcurrentDictionary<string, byte> { [groupName] = 0 },
            (_, groups) => { groups[groupName] = 0; return groups; });
    }

    private static void UntrackGroup(string connectionId, string groupName)
    {
        if (ConnectionGroups.TryGetValue(connectionId, out var groups))
        {
            groups.TryRemove(groupName, out _);
        }
    }
}
