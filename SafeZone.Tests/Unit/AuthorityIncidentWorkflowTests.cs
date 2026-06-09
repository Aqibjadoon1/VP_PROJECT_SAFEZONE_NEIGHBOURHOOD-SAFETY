using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SafeZone.Server.Controllers;
using Xunit;

namespace SafeZone.Tests.Unit;

public class AuthorityIncidentWorkflowTests
{
    [Fact]
    public void FieldReports_ViewAction_LoadsAndDisplaysIncidentDetails()
    {
        var pagePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SafeZone.Server", "Components", "Pages", "Authority", "FieldReports.razor"));
        var source = File.ReadAllText(pagePath);

        Assert.Contains("private async Task ViewIncident(Guid incidentId)", source);
        Assert.Contains("selectedIncidentDetail = await IncidentService.GetIncidentByIdAsync(incidentId)", source);
        Assert.Contains("Incident Details", source);
        Assert.Contains("@if (selectedIncidentDetail is not null)", source);
    }

    [Theory]
    [InlineData("GetAllIncidents")]
    [InlineData("UpdateStatus")]
    [InlineData("GetStats")]
    public void IncidentAdminActions_AuthorizeAdminRole(string methodName)
    {
        var method = typeof(IncidentController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Contains("Admin", authorize!.Roles?.Split(',') ?? []);
    }
}
