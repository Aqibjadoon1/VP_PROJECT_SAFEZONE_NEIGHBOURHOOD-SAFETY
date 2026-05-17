namespace SafeZone.Server.Models;

public enum UserRole
{
    Resident,
    Authority,
    SuperAdmin
}

public enum IncidentStatus
{
    Pending,
    Assigned,
    InProgress,
    Resolved,
    Closed
}

public enum SeverityLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum FIRStatus
{
    Submitted,
    UnderReview,
    Accepted,
    Rejected,
    Investigating,
    Closed
}

public enum AuthorityType
{
    Police,
    Ambulance,
    FireBrigade,
    TrafficPolice
}

public enum CallStatus
{
    Initiated,
    Ringing,
    Answered,
    Completed,
    Failed,
    NoAnswer,
    Cancelled
}

public enum AlertType
{
    Emergency,
    Warning,
    Info,
    WeatherAlert,
    CurfewNotice
}

public enum AlertScope
{
    Citywide,
    Radius,
    SpecificArea,
    AllAuthorities
}
