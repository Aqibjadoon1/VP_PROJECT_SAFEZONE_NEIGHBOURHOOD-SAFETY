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
