using SafeZone.Server.Models;
using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Unit;

public class StatusPresentationTests
{
    [Theory]
    [InlineData(IncidentStatus.Pending, "Pending", "pending")]
    [InlineData(IncidentStatus.InProgress, "In Progress", "inprogress")]
    [InlineData(FIRStatus.UnderReview, "Under Review", "warning")]
    [InlineData(CallStatus.NoAnswer, "No Answer", "offline")]
    public void From_KnownStatuses_ReturnsCleanBadgeMetadata(object status, string label, string cssClass)
    {
        var presentation = StatusPresenter.From(status);

        Assert.Equal(label, presentation.Label);
        Assert.Equal(cssClass, presentation.CssClass);
    }

    [Theory]
    [InlineData("return getStatusColor(status)")]
    [InlineData("Pending switch { nameof(IncidentStatus.Pending) => \"Pending\" }")]
    [InlineData("{ status: \"active\", color: \"green\" }")]
    public void From_RawCodeOrObjectText_ReturnsUnknownInsteadOfRenderingRawText(string rawStatus)
    {
        var presentation = StatusPresenter.From(rawStatus);

        Assert.Equal("Unknown", presentation.Label);
        Assert.Equal("unknown", presentation.CssClass);
    }
}
