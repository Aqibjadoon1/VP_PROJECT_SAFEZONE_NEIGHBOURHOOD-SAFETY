using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public sealed record StatusPresentation(string Label, string CssClass);

public static class StatusPresenter
{
    public static StatusPresentation From(object? status)
    {
        return status switch
        {
            IncidentStatus.Pending => new("Pending", "pending"),
            IncidentStatus.Assigned => new("Assigned", "active"),
            IncidentStatus.InProgress => new("In Progress", "inprogress"),
            IncidentStatus.Resolved => new("Resolved", "active"),
            IncidentStatus.Closed => new("Closed", "offline"),

            FIRStatus.Submitted => new("Submitted", "pending"),
            FIRStatus.UnderReview => new("Under Review", "warning"),
            FIRStatus.Accepted => new("Accepted", "active"),
            FIRStatus.Rejected => new("Rejected", "critical"),
            FIRStatus.Investigating => new("Investigating", "warning"),
            FIRStatus.Closed => new("Closed", "offline"),

            CallStatus.Initiated => new("Initiated", "pending"),
            CallStatus.Ringing => new("Ringing", "warning"),
            CallStatus.Answered => new("Answered", "active"),
            CallStatus.Completed => new("Completed", "active"),
            CallStatus.Failed => new("Failed", "critical"),
            CallStatus.NoAnswer => new("No Answer", "offline"),
            CallStatus.Cancelled => new("Cancelled", "offline"),

            bool isActive => isActive ? new("Active", "active") : new("Suspended", "suspended"),
            string text => FromText(text),
            null => new("Unknown", "unknown"),
            _ => new("Unknown", "unknown")
        };
    }

    private static StatusPresentation FromText(string text)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || LooksLikeRawCode(normalized))
        {
            return new("Unknown", "unknown");
        }

        return normalized.Replace(" ", string.Empty).ToLowerInvariant() switch
        {
            "active" or "resolved" or "accepted" or "answered" or "completed" => new(ToLabel(normalized), "active"),
            "pending" or "submitted" or "initiated" => new(ToLabel(normalized), "pending"),
            "offline" or "closed" or "cancelled" or "inactive" => new(ToLabel(normalized), "offline"),
            "warning" or "underreview" or "investigating" or "ringing" => new(ToLabel(normalized), "warning"),
            "critical" or "failed" or "rejected" => new(ToLabel(normalized), "critical"),
            "suspended" or "deactivated" => new("Suspended", "suspended"),
            "inprogress" => new("In Progress", "inprogress"),
            _ => new("Unknown", "unknown")
        };
    }

    private static bool LooksLikeRawCode(string value)
    {
        return value.Contains('{') ||
               value.Contains('}') ||
               value.Contains("=>", StringComparison.Ordinal) ||
               value.Contains("return ", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("switch", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("nameof", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("status:", StringComparison.OrdinalIgnoreCase) ||
               value.Length > 40;
    }

    private static string ToLabel(string value)
    {
        return value.Replace("_", " ").Replace("-", " ") switch
        {
            var v when string.Equals(v, "inprogress", StringComparison.OrdinalIgnoreCase) => "In Progress",
            var v when string.Equals(v, "underreview", StringComparison.OrdinalIgnoreCase) => "Under Review",
            var v when string.Equals(v, "noanswer", StringComparison.OrdinalIgnoreCase) => "No Answer",
            var v => string.Join(' ', v.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()))
        };
    }
}
