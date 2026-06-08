using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SafeZone.Server.Hubs;

[Authorize]
public class MapHub : Hub
{
    private static readonly ConcurrentDictionary<string, (double Lat, double Lng)> _userLocations = new();

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
        if (!IsAuthorityOrHigher(Context.User))
        {
            return;
        }

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
        if (!IsAuthorityOrHigher(Context.User))
        {
            return;
        }

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
            _userLocations.TryRemove(userId, out _);
            await Clients.Others.SendAsync("UserOffline", new { UserId = userId });
        }
        await base.OnDisconnectedAsync(exception);
    }

    private static bool IsAuthorityOrHigher(System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user is null) return false;
        return user.IsInRole("Authority") || user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
    }
}
