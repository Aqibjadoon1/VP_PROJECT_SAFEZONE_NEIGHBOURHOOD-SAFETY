using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SafeZone.Server.Hubs;

[Authorize]
public class AlertHub : Hub
{
    public async Task JoinAuthorityGroup()
    {
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
        await Clients.Group("authorities").SendAsync("EmergencyCallRequested", new
        {
            IncidentId = incidentId,
            CallerInfo = callerInfo,
            Timestamp = DateTime.UtcNow
        });
    }
}
