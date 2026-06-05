using SafeZone.Server.Models;
using Xunit;

namespace SafeZone.Tests.Unit;

public class IncidentServiceTests
{
    [Fact]
    public void IncidentStatusEnum_HasAllValues()
    {
        var values = Enum.GetValues<IncidentStatus>();
        Assert.Equal(5, values.Length);
    }

    [Fact]
    public void SeverityLevelEnum_HasAllValues()
    {
        var values = Enum.GetValues<SeverityLevel>();
        Assert.Equal(4, values.Length);
    }

    [Fact]
    public void Incident_Creation_SetsDefaults()
    {
        var incident = new Incident
        {
            Title = "Test",
            Description = "Test desc"
        };

        Assert.Equal("Test", incident.Title);
        Assert.Equal(IncidentStatus.Pending, incident.Status);
    }
}
