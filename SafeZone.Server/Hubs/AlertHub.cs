using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.Encodings.Web;

namespace SafeZone.Server.Hubs;

[Authorize]
public class AlertHub : Hub
{
    public async Task JoinAuthorityGroup()
    {
        if (!IsAuthorityOrHigher(Context.User))
        {
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, "authorities");
    }

    public async Task LeaveAuthorityGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "authorities");
    }

    public async Task JoinLocationArea(double lat, double lng, double radiusKm = 2.0)
    {
        var areaKey = $"area_{lat:F2}_{lng:F2}";
        await Groups.AddToGroupAsync(Context.ConnectionId, areaKey);
    }

    public async Task LeaveLocationArea(double lat, double lng)
    {
        var areaKey = $"area_{lat:F2}_{lng:F2}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, areaKey);
    }

    public async Task BroadcastAlert(string alertType, string title, string message)
    {
        if (!IsAuthorityOrHigher(Context.User))
        {
            return;
        }

        await Clients.All.SendAsync("ReceiveAlert", new
        {
            AlertType = alertType,
            Title = title,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task SendEmergencyCallRequest(Guid incidentId, string callerInfo)
    {
        if (Context.User is null)
        {
            return;
        }

        await Clients.Group("authorities").SendAsync("EmergencyCallRequested", new
        {
            IncidentId = incidentId,
            CallerInfo = callerInfo,
            Timestamp = DateTime.UtcNow
        });
    }

    private static bool IsAuthorityOrHigher(System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user is null) return false;
        return user.IsInRole("Authority") || user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
    }
}
