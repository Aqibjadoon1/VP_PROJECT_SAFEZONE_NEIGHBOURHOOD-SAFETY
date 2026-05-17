using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SafeZone.Server.Hubs;

[Authorize]
public class MapHub : Hub
{
    private static readonly Dictionary<string, (double Lat, double Lng)> _userLocations = new();

    public async Task UpdateLocation(double lat, double lng)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _userLocations[userId] = (lat, lng);
            await Clients.Others.SendAsync("UserLocationUpdated", new
            {
                UserId = userId,
                Lat = lat,
                Lng = lng,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task ReportNewIncident(Guid incidentId, double lat, double lng, string type, string severity)
    {
        await Clients.All.SendAsync("NewIncidentReported", new
        {
            IncidentId = incidentId,
            Lat = lat,
            Lng = lng,
            Type = type,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task IncidentResolved(Guid incidentId)
    {
        await Clients.All.SendAsync("IncidentResolved", new
        {
            IncidentId = incidentId,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _userLocations.Remove(userId);
            await Clients.Others.SendAsync("UserOffline", new { UserId = userId });
        }
        await base.OnDisconnectedAsync(exception);
    }
}
