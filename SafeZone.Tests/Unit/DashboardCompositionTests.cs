using Xunit;

namespace SafeZone.Tests.Unit;

public class DashboardCompositionTests
{
    [Fact]
    public void ResidentDashboard_DoesNotRepeatLayoutOwnedHeaderOrStatusBar()
    {
        var dashboardPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SafeZone.Server", "Components", "Pages", "User", "Dashboard.razor"));
        var dashboardSource = File.ReadAllText(dashboardPath);

        Assert.DoesNotContain("<PageHeader", dashboardSource);
        Assert.DoesNotContain("<StatusBar", dashboardSource);
    }
}
