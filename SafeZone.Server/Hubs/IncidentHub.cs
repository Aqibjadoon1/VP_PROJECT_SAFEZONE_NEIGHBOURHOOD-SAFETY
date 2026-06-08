using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.Encodings.Web;

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
        var sanitizedMessage = HtmlEncoder.Default.Encode(message);

        await Clients.Group($"incident_{incidentId}").SendAsync("ReceiveComment", new
        {
            IncidentId = incidentId,
            UserId = Context.UserIdentifier,
            Message = sanitizedMessage,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task SendStatusUpdate(Guid incidentId, string status, string? notes = null)
    {
        if (!IsAuthorityOrHigher(Context.User))
        {
            return;
        }

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

    private static bool IsAuthorityOrHigher(System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user is null) return false;
        return user.IsInRole("Authority") || user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
    }
}
