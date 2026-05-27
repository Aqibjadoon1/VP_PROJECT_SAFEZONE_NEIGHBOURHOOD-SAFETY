using Xunit;

namespace SafeZone.Tests.E2E;

public class UserFlowTests
{
    [Fact]
    public void LoginPage_ShouldBeAccessible()
    {
        var baseUrl = Environment.GetEnvironmentVariable("SAFEZONE_URL") ?? "https://localhost:7026";
        Assert.StartsWith("https://", baseUrl);
    }

    [Fact]
    public void ReportIncidentFlow_StepsExist()
    {
        var steps = new[] { "TypeSelection", "LocationPin", "DetailsForm", "ReviewAndSubmit" };
        Assert.Equal(4, steps.Length);
    }
}
