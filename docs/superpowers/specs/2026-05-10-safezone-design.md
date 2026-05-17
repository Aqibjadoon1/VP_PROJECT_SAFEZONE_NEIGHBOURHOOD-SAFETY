# SafeZone Design Specification

**Date:** 2026-05-10  
**Project:** SafeZone — Neighborhood Safety & Incident Reporting System  
**Course:** CS-284L Visual Programming Lab, Air University Islamabad  
**Team:** Siddique Akbar (241826), Aqib Jadoon (241916), Talha Muhammad Yasin (241874)  
**Target Grade:** A+

---

## 1. Architecture Overview

### 1.1 Project Structure

```
SafeZone/
├── SafeZone.sln
│
├── SafeZone.Server/                    ← ASP.NET Core 8 Web API + SignalR
│   ├── SafeZone.Server.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── IncidentController.cs
│   │   ├── MapController.cs
│   │   ├── AlertController.cs
│   │   ├── WeatherController.cs
│   │   ├── NotificationController.cs
│   │   ├── FIRController.cs
│   │   ├── AIAgentController.cs
│   │   ├── UserController.cs
│   │   ├── AuthorityController.cs
│   │   ├── ReportController.cs
│   │   └── AdminController.cs
│   ├── Hubs/
│   │   ├── IncidentHub.cs
│   │   ├── AlertHub.cs
│   │   └── MapHub.cs
│   ├── Models/
│   │   ├── User.cs (extends IdentityUser<Guid>)
│   │   ├── Incident.cs
│   │   ├── Authority.cs
│   │   ├── Response.cs
│   │   ├── Comment.cs
│   │   ├── FIRReport.cs
│   │   ├── Alert.cs
│   │   ├── Notification.cs
│   │   ├── ProximityAlert.cs
│   │   ├── AICallLog.cs
│   │   └── IncidentCategory.cs
│   ├── DTOs/
│   ├── Services/
│   │   ├── IIncidentService.cs + IncidentService.cs
│   │   ├── IFIRService.cs + FIRService.cs
│   │   ├── IAICallingAgentService.cs + AICallingAgentService.cs
│   │   ├── IWeatherService.cs + WeatherService.cs
│   │   ├── INotificationService.cs + NotificationService.cs
│   │   ├── IProximityService.cs + ProximityService.cs
│   │   ├── IPDFGeneratorService.cs + PDFGeneratorService.cs
│   │   ├── ISeverityTaggerService.cs + SeverityTaggerService.cs
│   │   └── IAuthService.cs + AuthService.cs
│   ├── Data/
│   │   ├── SafeZoneDbContext.cs
│   │   ├── Migrations/
│   │   └── SeedData.cs
│   ├── Middleware/
│   │   ├── JwtMiddleware.cs
│   │   ├── RateLimitMiddleware.cs
│   │   └── ExceptionHandlingMiddleware.cs
│   └── Helpers/
│       ├── GeoHelper.cs
│       ├── JwtHelper.cs
│       └── AnonymousHelper.cs
│
├── SafeZone.Client/                   ← Pure HTML + CSS (Tailwind) + JavaScript
│   ├── index.html                     ← Landing (public)
│   ├── login.html
│   ├── register.html
│   ├── user/                          ← RESIDENT DASHBOARD
│   │   ├── dashboard.html
│   │   ├── report-incident.html
│   │   ├── file-fir.html
│   │   ├── my-incidents.html
│   │   ├── notifications.html
│   │   ├── sos.html
│   │   └── profile.html
│   ├── authority/                     ← AUTHORITY DASHBOARD
│   │   ├── dashboard.html
│   │   ├── incidents.html
│   │   ├── kanban.html
│   │   ├── heatmap.html
│   │   ├── fir-management.html
│   │   ├── broadcast-alert.html
│   │   ├── ai-agent.html
│   │   ├── analytics.html
│   │   └── reports.html
│   ├── css/
│   │   ├── global.css
│   │   ├── components.css
│   │   ├── map.css
│   │   ├── animations.css
│   │   └── dashboard.css
│   ├── js/
│   │   ├── api.js
│   │   ├── auth.js
│   │   ├── signalr-client.js
│   │   ├── map.js
│   │   ├── heatmap.js
│   │   ├── weather.js
│   │   ├── sos.js
│   │   ├── fir.js
│   │   ├── kanban.js
│   │   ├── charts.js
│   │   ├── notifications.js
│   │   ├── geolocation.js
│   │   └── ai-agent.js
│   └── assets/
│       ├── icons/
│       └── images/
│
├── tailwind.config.js
├── package.json
└── README.md
```

### 1.2 Technology Stack

| Layer | Technology |
|-------|------------|
| **Backend Framework** | ASP.NET Core 8 Web API |
| **Database** | SQL Server LocalDB + Entity Framework Core 8 (Code-First) |
| **Auth** | ASP.NET Core Identity + JWT Bearer |
| **Real-time** | SignalR |
| **Maps** | Leaflet.js + Leaflet.heat + CartoDB Dark Matter tiles |
| **Charts** | Chart.js |
| **3D Hero** | Three.js |
| **CSS** | Tailwind CSS v3 (CDN or PostCSS) |
| **AI** | OpenAI GPT-4o (script generation) |
| **Voice/SMS** | Twilio API (mock mode available) |
| **Weather** | OpenWeatherMap API |
| **PDF** | QuestPDF |
| **Fonts (Google)** | Syne (display), DM Sans (UI), JetBrains Mono (data) |

---

## 2. Design System

### 2.1 Color Palette

```css
:root {
    --bg-void:         #0A0A14;   /* near-black with indigo tinge */
    --bg-deep:         #0D0D1A;   /* matches reference image */
    --bg-surface:      #12121F;   /* card backgrounds */
    --bg-elevated:     #1A1A2E;
    --glass:           rgba(255,255,255,0.04);
    --glass-hover:     rgba(255,255,255,0.08);
    --glass-border:    rgba(255,255,255,0.10);
    --glass-border-strong: rgba(255,255,255,0.20);

    --green:           #00FF88;   /* PRIMARY ACCENT */
    --green-dim:       rgba(0,255,136,0.15);
    --green-glow:      rgba(0,255,136,0.3);

    --red-alert:       #FF3B5C;   /* danger, emergency */
    --orange-warn:     #FF9500;   /* warning */
    --blue-info:       #3B82F6;   /* info, progress */
    --purple:          #A855F7;

    --text-1:          #FFFFFF;
    --text-2:          rgba(255,255,255,0.70);
    --text-3:          rgba(255,255,255,0.40);

    --glow-purple:     rgba(99, 51, 255, 0.3);   /* ambient glow */
    --glow-indigo:     rgba(79, 70, 229, 0.2);
}
```

### 2.2 Typography

| Use Case | Font | Google Fonts |
|----------|------|--------------|
| Display/Headings | Syne | `'Syne', sans-serif` (weights: 400, 600, 700, 800) |
| UI/Body | DM Sans | `'DM Sans', sans-serif` (weights: 300, 400, 500, 600) |
| Numbers/Data | JetBrains Mono | `'JetBrains Mono', monospace` (weights: 400, 600) |

### 2.3 Global Effects (ALL Pages)

1. **Dot-grid texture overlay** - `background-image: radial-gradient(circle, rgba(255,255,255,0.06) 1px, transparent 1px)` at 32px spacing
2. **Ambient purple/indigo glow blob** - `position: fixed, blur: 200px, pointer-events: none`
3. **Glassmorphism on ALL cards** - `backdrop-filter: blur(16px) saturate(180%)`
4. **Custom scrollbar** - thin, green thumb on dark track
5. **Smooth page transitions** - fade + slight translateY on load
6. **Cursor rules** - crosshair on map areas, pointer on interactive elements

### 2.4 Severity Colors

| Severity | Background | Border |
|----------|------------|--------|
| **Critical** | `rgba(255,59,92,0.2)` | `rgba(255,59,92,0.4)` |
| **High** | `rgba(255,149,0,0.2)` | `rgba(255,149,0,0.4)` |
| **Medium** | `rgba(59,130,246,0.2)` | `rgba(59,130,246,0.4)` |
| **Low** | `rgba(0,255,136,0.1)` | `rgba(0,255,136,0.3)` |

---

## 3. Database Models (EF Core)

### 3.1 User (extends IdentityUser<Guid>)

```csharp
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; }
    public UserRole Role { get; set; }                  // Resident, Authority, SuperAdmin
    public string PhoneHash { get; set; }               // Hashed for anonymous mode
    public double? LastKnownLatitude { get; set; }
    public double? LastKnownLongitude { get; set; }
    public double ProximityRadiusKm { get; set; } = 2.0;
    public bool IsAnonymous { get; set; } = false;
    public bool PushNotificationsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Incident> ReportedIncidents { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<Notification> Notifications { get; set; }
    public ICollection<FIRReport> FIRReports { get; set; }
}

public enum UserRole { Resident, Authority, SuperAdmin }
```

### 3.2 Incident

```csharp
public class Incident
{
    public Guid IncidentId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? ReporterId { get; set; }              // null = anonymous
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public IncidentStatus Status { get; set; }
    public SeverityLevel Severity { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsFIRFiled { get; set; }
    public string? EvidenceUrls { get; set; }          // JSON array
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? AssignedAuthorityId { get; set; }
    public string? AIGeneratedSummary { get; set; }
    public string? AICallLogId { get; set; }

    // Navigation
    public User Reporter { get; set; }
    public IncidentCategory Category { get; set; }
    public Authority AssignedAuthority { get; set; }
    public ICollection<Response> Responses { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public FIRReport FIR { get; set; }
}

public enum IncidentStatus { Pending, Assigned, InProgress, Resolved, Closed }
public enum SeverityLevel { Low, Medium, High, Critical }
```

### 3.3 FIRReport

```csharp
public class FIRReport
{
    public Guid FIRId { get; set; }
    public string FIRNumber { get; set; }               // FIR-2026-001234
    public Guid IncidentId { get; set; }
    public Guid ReporterId { get; set; }
    public string ComplainantName { get; set; }
    public string ComplainantCNIC { get; set; }         // hashed
    public string ComplainantPhone { get; set; }
    public string ComplainantAddress { get; set; }
    public string AccusedDescription { get; set; }
    public string IncidentNarrative { get; set; }
    public string WitnessDetails { get; set; }
    public string PropertyLost { get; set; }
    public double EstimatedLoss { get; set; }
    public FIRStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAuthorityId { get; set; }
    public string? PDFUrl { get; set; }

    public Incident Incident { get; set; }
    public User Reporter { get; set; }
}

public enum FIRStatus { Submitted, UnderReview, Accepted, Rejected }
```

### 3.4 Authority

```csharp
public class Authority
{
    public Guid AuthId { get; set; }
    public Guid UserId { get; set; }
    public string UnitName { get; set; }
    public string BadgeNumber { get; set; }
    public string JurisdictionGeoJson { get; set; }
    public double JurisdictionCenterLat { get; set; }
    public double JurisdictionCenterLng { get; set; }
    public string ContactInfo { get; set; }
    public string EmergencyPhone { get; set; }
    public bool IsOnDuty { get; set; }
    public AuthorityType Type { get; set; }

    public User User { get; set; }
    public ICollection<Response> Responses { get; set; }
}

public enum AuthorityType { Police, Ambulance, FireBrigade, TrafficPolice }
```

### 3.5 AICallLog

```csharp
public class AICallLog
{
    public Guid LogId { get; set; }
    public Guid IncidentId { get; set; }
    public Guid TriggeredByUserId { get; set; }
    public string CallType { get; set; }           // Police, Ambulance, FireBrigade
    public string PhoneNumberCalled { get; set; }
    public string TwilioCallSid { get; set; }
    public string AIScript { get; set; }
    public CallStatus Status { get; set; }
    public DateTime InitiatedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string TranscriptUrl { get; set; }

    public Incident Incident { get; set; }
}

public enum CallStatus { Initiated, Ringing, Answered, Completed, Failed, NoAnswer }
```

### 3.6 Alert

```csharp
public class Alert
{
    public Guid AlertId { get; set; }
    public Guid IssuedByAuthorityId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public AlertType Type { get; set; }
    public AlertScope Scope { get; set; }
    public double? RadiusKm { get; set; }
    public double? CenterLat { get; set; }
    public double? CenterLng { get; set; }
    public string? TargetGeoJson { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }

    public Authority IssuedBy { get; set; }
}

public enum AlertType { Emergency, Warning, Info, WeatherAlert, CurfewNotice }
public enum AlertScope { Citywide, Radius, SpecificArea, AllAuthorities }
```

### 3.7 IncidentCategory (Seeded)

```csharp
public class IncidentCategory
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public string Description { get; set; }
}
```

**Seeded Categories:** Theft, Robbery, Vandalism, Accident, Fire, Medical Emergency, Harassment, Suspicious Activity, Missing Person, Other

---

## 4. Core Features Implementation Order

### Phase 1: Foundations
1. Create solution + project structure
2. Program.cs configuration
3. appsettings.json (with placeholders for API keys)
4. All EF Core entities + SafeZoneDbContext
5. Create initial migration + update database
6. SeedData.cs (roles, users, sample incidents)

### Phase 2: Auth & API Core
7. AuthController (Register, Login, RefreshToken, Logout)
8. JWT authentication setup
9. SignalR Hubs (IncidentHub, AlertHub, MapHub)
10. ExceptionHandlingMiddleware

### Phase 3: Frontend Foundations
11. Tailwind config + global.css (full design system)
12. Login + Register pages
13. Landing page (index.html) with Three.js 3D sphere

### Phase 4: Core Services
14. IncidentController + IncidentService
15. SeverityTaggerService (keyword-based + optional GPT-4o)
16. WeatherService (OpenWeatherMap proxy with caching)
17. MapController (incidents, heatmap)

### Phase 5: User Dashboard
18. User dashboard layout (sidebar + main)
19. Leaflet map initialization (user/map.js)
20. Weather widget
21. Report incident form (multi-step)
22. My incidents page
23. Notifications page

### Phase 6: SOS + AI Calling Agent
24. SOS page (sos.html)
25. AIAgentController
26. AICallingAgentService (Twilio integration + mock mode)
27. SignalR call status updates

### Phase 7: FIR System
28. FIRController + FIRService
29. File FIR form (4-step)
30. PDFGeneratorService (QuestPDF)
31. FIR tracking

### Phase 8: Authority Dashboard
32. Authority dashboard layout
33. Kanban board (drag-drop incident management)
34. Authority map view
35. Broadcast Alert page
36. AI Agent control panel
37. Analytics (Chart.js)
38. Reports page

### Phase 9: Polish & Testing
39. All mobile-responsive breakpoints
40. Loading states + empty states + error states
41. Confirmation dialogs
42. Viva demo flow testing

---

## 5. External Dependencies

| Service | Purpose | Mock Mode Available |
|---------|---------|---------------------|
| **OpenWeatherMap API** | Weather data + 5-day forecast | Yes (static mock responses) |
| **Twilio API** | Voice calls + SMS | Yes (console logging + DB records) |
| **OpenAI GPT-4o** | Severity classification + call script generation | Yes (keyword-based fallback) |

**Pakistan Emergency Numbers:**
- Police: +92 15
- Ambulance/Rescue: +92 1122
- Fire Brigade: +92 16

---

## 6. appsettings.json Template

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SafeZoneDb;Trusted_Connection=True;"
  },
  "Jwt": {
    "Secret": "SafeZone_SuperSecretKey_32CharsMin_AirUniversity",
    "Issuer": "SafeZone.Api",
    "Audience": "SafeZone.Client",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Twilio": {
    "AccountSid": "",
    "AuthToken": "",
    "FromNumber": "",
    "MockMode": true
  },
  "OpenAI": {
    "ApiKey": "",
    "Model": "gpt-4o"
  },
  "OpenWeatherMap": {
    "ApiKey": ""
  },
  "Emergency": {
    "PoliceNumber": "+9215",
    "AmbulanceNumber": "+921122",
    "FireNumber": "+9216"
  },
  "Storage": {
    "EvidenceBasePath": "wwwroot/evidence",
    "FIRBasePath": "wwwroot/firs",
    "MaxFileSizeMB": 10
  },
  "Proximity": {
    "DefaultRadiusKm": 2.0,
    "MaxRadiusKm": 10.0
  }
}
```

---

## 7. NuGet Packages (Server)

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
<PackageReference Include="Twilio" Version="7.0.0" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.0.0" />
<PackageReference Include="QuestPDF" Version="2024.3.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
```

---

## 8. npm Packages (Client)

```json
{
  "dependencies": {
    "leaflet": "^1.9.4",
    "leaflet.heat": "^0.2.0",
    "leaflet.markercluster": "^1.5.3",
    "leaflet-draw": "^1.0.4",
    "@microsoft/signalr": "^8.0.0",
    "chart.js": "^4.4.0",
    "sortablejs": "^1.15.0",
    "three": "^0.160.0"
  },
  "devDependencies": {
    "tailwindcss": "^3.4.0",
    "autoprefixer": "^10.4.0"
  }
}
```

---

## 9. Seeded Credentials (for Demo)

| Role | Email | Password |
|------|-------|----------|
| **SuperAdmin** | `admin@safezone.pk` | `Admin123!` |
| **Authority (Police)** | `officer@safezone.pk` | `Officer123!` |
| **Authority (Ambulance)** | `rescue@safezone.pk` | `Rescue123!` |
| **Resident** | `user@safezone.pk` | `User123!` |

---

## 10. Viva Demo Flow

1. **Landing Page** - Show 3D sphere spinning, animated counters, glassmorphism cards
2. **Register + Login** - Create new resident user, then log in
3. **User Dashboard** - Show live Leaflet map, custom markers, heatmap toggle
4. **Weather Widget** - Show conditions + safety context message
5. **Report Incident** - Multi-step form, pick location on map
6. **Real-time Update** - Open authority dashboard in second tab, show new marker appear
7. **Proximity Alert** - Show notification toast on user dashboard
8. **File FIR** - Complete 4-step form, download generated PDF
9. **SOS Emergency** - Click Ambulance button, show AI calling agent UI with live status
10. **Authority Kanban** - Drag incident card to show status update
11. **Broadcast Alert** - Send emergency alert, show it appear on resident side
12. **Analytics** - Show Chart.js visualizations
13. **AI Agent Panel** - Show call history, script preview

---

## 11. Aesthetic North Star

**Design Principle:** Every page must feel like a premium dark-tech product, not a student project.

**Reference Image Energy:**
- Deep dark background (#0A0A14) with purple ambient glow
- Massive glowing 3D element as hero
- Electric green (#00FF88) as ONLY accent color
- Glassmorphism on ALL cards
- Clean, minimal, professional

**On Dashboards:**
- The map IS the hero element
- Glass cards float OVER the map
- Green = active, CTAs, safe, resolved
- Red = SOS, critical, danger
- Everything else = white on black
