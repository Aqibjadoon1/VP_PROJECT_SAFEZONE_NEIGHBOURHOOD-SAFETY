using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SafeZone.Server.Hubs;

[Authorize]
public class CallHub : Hub
{
    public const string AuthoritiesGroup = "call-monitors";
    private static readonly ConcurrentDictionary<string, HashSet<string>> ConnectionGroups = new();

    public async Task JoinCallMonitoring()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AuthoritiesGroup);
        TrackGroup(Context.ConnectionId, AuthoritiesGroup);
    }

    public async Task LeaveCallMonitoring()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AuthoritiesGroup);
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
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionGroups.TryRemove(Context.ConnectionId, out var groups))
        {
            foreach (var group in groups)
            {
                try { await Groups.RemoveFromGroupAsync(Context.ConnectionId, group); }
                catch { }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private static void TrackGroup(string connectionId, string groupName)
    {
        ConnectionGroups.AddOrUpdate(
            connectionId,
            _ => new HashSet<string> { groupName },
            (_, groups) => { groups.Add(groupName); return groups; });
    }
}
