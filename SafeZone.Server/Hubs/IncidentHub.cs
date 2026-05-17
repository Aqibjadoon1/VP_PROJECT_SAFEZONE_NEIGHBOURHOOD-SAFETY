using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SafeZone.Server.Hubs;

[Authorize]
public class IncidentHub : Hub
{
    public async Task JoinIncidentRoom(Guid incidentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"incident_{incidentId}");
    }

    public async Task LeaveIncidentRoom(Guid incidentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"incident_{incidentId}");
    }

    public async Task SendComment(Guid incidentId, string message)
    {
        await Clients.Group($"incident_{incidentId}").SendAsync("ReceiveComment", new
        {
            IncidentId = incidentId,
            UserId = Context.UserIdentifier,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task SendStatusUpdate(Guid incidentId, string status, string? notes = null)
    {
        await Clients.Group($"incident_{incidentId}").SendAsync("ReceiveStatusUpdate", new
        {
            IncidentId = incidentId,
            Status = status,
            Notes = notes,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
