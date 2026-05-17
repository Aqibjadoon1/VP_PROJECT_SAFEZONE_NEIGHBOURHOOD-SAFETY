# SafeZone — Neighborhood Safety System

## CS-284L Visual Programming Lab | Air University Islamabad | Spring 2026

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture](#architecture)
4. [Complete File Structure](#complete-file-structure)
5. [Database Design](#database-design)
6. [REST API Reference](#rest-api-reference)
7. [SignalR Real-Time Hubs](#signalr-real-time-hubs)
8. [Authentication & Authorization](#authentication--authorization)
9. [Voice AI Pipeline](#voice-ai-pipeline)
10. [External Integrations](#external-integrations)
11. [Team Division — Who Did What](#team-division--who-did-what)
12. [Feature Status](#feature-status)
13. [Setup & Running](#setup--running)

---

## Project Overview

**SafeZone** is a full-stack neighborhood safety platform that connects residents with emergency services through real-time incident reporting, AI-powered emergency voice calls, live neighborhood mapping, and a tactical command-center interface for authorities.

| Attribute | Value |
|-----------|-------|
| **Framework** | Blazor Server (.NET 8.0) |
| **Language** | C# 12, JavaScript, CSS |
| **Database** | SQLite via Entity Framework Core 8 |
| **Real-Time** | SignalR (4 hubs) |
| **Auth** | ASP.NET Core Identity (Cookie + JWT + Google OAuth) |
| **Voice AI** | STT → LLM → TTS pipeline (Groq + ElevenLabs) |
| **Maps** | Leaflet.js + leaflet-heat |
| **3D** | Three.js (landing page) |
| **CSS** | Tailwind CDN + 4100-line custom tactical design system |

---

## Technology Stack

```
┌─────────────────────────────────────────────────────────┐
│  PRESENTATION LAYER                                     │
│  Blazor Server Components (.razor) + Razor Pages        │
│  Tailwind CSS + Custom Tactical Design System           │
│  Leaflet.js · Three.js · ElevenLabs Convai              │
├─────────────────────────────────────────────────────────┤
│  REAL-TIME LAYER                                        │
│  SignalR Hubs: IncidentHub · AlertHub · MapHub · CallHub│
├─────────────────────────────────────────────────────────┤
│  SERVICE LAYER                                          │
│  Auth · Incident · Alert · FIR · SOS · SMS · Voice      │
│  VoicePipeline (STT→LLM→TTS)                            │
├─────────────────────────────────────────────────────────┤
│  DATA LAYER                                             │
│  Entity Framework Core 8 · SQLite                       │
│  ASP.NET Core Identity (Guid PK)                        │
├─────────────────────────────────────────────────────────┤
│  EXTERNAL                                               │
│  Groq LLM API · ElevenLabs Voice Agent · Open-Meteo     │
│  Google OAuth · ONNX Runtime · OpenAI SDK               │
│  Gmail API (planned) · Slack Webhooks (planned)         │
└─────────────────────────────────────────────────────────┘
```

---

## Architecture

```
                        _Host.cshtml
                             │
                        App.razor
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
    MainLayout         LoginLayout       DashboardLayout
    (public pages)     (auth pages)      (authenticated)
          │                  │                  │
    ┌─────┴─────┐    ┌─────┴─────┐    ┌───────┴────────┐
    │Index      │    │Login      │    │ User Pages (10) │
    │NotFound   │    │Register   │    │ Authority (9)   │
    │AccessDen  │    │           │    │ Shared Components│
    └───────────┘    └───────────┘    └────────────────┘
                                             │
    ┌────────────────────────────────────────┼──────────────────────┐
    │                                        │                      │
    ▼                                        ▼                      ▼
 Controllers (9)                     Services (16+)           SignalR Hubs (4)
 REST API endpoints                  Business logic           Real-time events
```

### Data Flow

```
Browser (Blazor Server via SignalR circuit)
    │
    ├── Component events → C# methods → Service layer → EF Core → SQLite
    │
    ├── REST API calls → Controllers → Services → DB
    │
    ├── SignalR → Hubs → broadcast to all connected clients
    │
    └── JS Interop → Leaflet maps, Three.js 3D, Voice widgets
```

---

## Complete File Structure

```
VISUAL PROGRAMMING PROJECT/
│
├── SafeZone.slnx                              ← Solution file
├── README2.md                                 ← THIS FILE
│
├── docs/
│   └── superpowers/
│       ├── plans/                             ← 7 implementation plans
│       └── specs/                             ← 6 design specification docs
│
├── SafeZone.Server/                           ← ★ MAIN APPLICATION
│   ├── SafeZone.Server.csproj                 ← .NET 8 project (all NuGet refs)
│   ├── Program.cs                             ← App entry (525 lines): DI, auth, hubs, pipeline
│   ├── App.razor                              ← Root component (router, auth views)
│   ├── _Imports.razor                         ← Global Razor usings
│   ├── SafeZone.Server.http                   ← HTTP test file
│   ├── SafeZone.db                            ← SQLite database file
│   │
│   ├── appsettings.json                       ← Connection strings, JWT config
│   ├── appsettings.Development.json           ← Groq/OpenAI keys, Google OAuth, Twilio mock
│   ├── Properties/launchSettings.json         ← Port config (http:5002, https:7026)
│   │
│   ├── Pages/
│   │   └── _Host.cshtml                       ← Blazor Server host: loads all CSS/JS/CDN
│   │
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor               ← Public layout (landing, error pages)
│   │   │   ├── LoginLayout.razor              ← Auth layout (login, register)
│   │   │   └── DashboardLayout.razor          ← Authenticated layout (sidebar, topbar, logout timer)
│   │   │
│   │   ├── Pages/
│   │   │   ├── Index.razor                    ← Landing page: 3D hero, features, tech stack
│   │   │   ├── Login.razor                    ← Tactical terminal login
│   │   │   ├── Register.razor                 ← Tactical terminal registration
│   │   │   ├── NotFound.razor                 ← 404 page
│   │   │   ├── AccessDenied.razor             ← 403 page
│   │   │   │
│   │   │   ├── User/
│   │   │   │   ├── Dashboard.razor            ← Resident main dashboard
│   │   │   │   ├── ReportIncident.razor       ← 4-step incident wizard
│   │   │   │   ├── MyIncidents.razor          ← Incident list + detail modal
│   │   │   │   ├── Sos.razor                  ← SOS emergency: countdown, call, result
│   │   │   │   ├── FileFir.razor              ← 4-step FIR filing wizard
│   │   │   │   ├── MyFirs.razor               ← Filed FIRs list
│   │   │   │   ├── Notifications.razor        ← Notification center
│   │   │   │   ├── WeatherMap.razor           ← Multi-layer heatmap map
│   │   │   │   ├── Profile.razor              ← User profile view/edit
│   │   │   │   └── Settings.razor             ← Notifications, privacy, proximity
│   │   │   │
│   │   │   └── Authority/
│   │   │       ├── AuthorityBoard.razor       ← Command center dashboard
│   │   │       ├── KanbanBoard.razor          ← Incident Kanban (5 columns)
│   │   │       ├── DispatchMap.razor          ← Live incident dispatch map
│   │   │       ├── FieldReports.razor         ← Incident management table
│   │   │       ├── FIRManagement.razor        ← FIR review/processing
│   │   │       ├── SosLogs.razor              ← SOS call log viewer
│   │   │       ├── AuthoritySettings.razor    ← Profile, broadcast alerts, SMS
│   │   │       ├── AiAgent.razor              ← AI chat assistant interface
│   │   │       └── UserManagement.razor       ← Admin: manage users, roles
│   │   │
│   │   └── Shared/
│   │       ├── AppSidebar.razor               ← Role-based nav sidebar
│   │       ├── Badge.razor                    ← Status badge chip
│   │       ├── Breadcrumbs.razor              ← Breadcrumb navigation
│   │       ├── ConfirmDialog.razor            ← Modal confirmation dialog
│   │       ├── EmptyState.razor               ← Empty state placeholder
│   │       ├── GlassCard.razor                ← Glassmorphism card wrapper
│   │       ├── IncidentCard.razor             ← Incident display card
│   │       ├── LoadingSkeleton.razor          ← Skeleton loading placeholders
│   │       ├── LoadingSpinner.razor           ← Spinning loader
│   │       ├── MetricCard.razor               ← Dashboard metric card
│   │       ├── PageHeader.razor               ← Page header (title + subtitle)
│   │       ├── RedirectToLogin.razor          ← Auth redirect handler
│   │       ├── SearchToolbar.razor            ← Search input toolbar
│   │       ├── SectionContainer.razor         ← Section wrapper with title
│   │       ├── SeverityChip.razor             ← Severity level chip
│   │       ├── StatCard.razor                 ← Statistic display card
│   │       ├── StatusBar.razor                ← Live clock + system status
│   │       ├── StatusChip.razor               ← Status indicator chip
│   │       └── Toast.razor                    ← Toast notification system
│   │
│   ├── Controllers/
│   │   ├── AuthController.cs                  ← Register, Login, Logout, Refresh, Me
│   │   ├── IncidentController.cs              ← CRUD, status updates, stats, categories
│   │   ├── AlertController.cs                 ← Create, list, deactivate, nearby
│   │   ├── FirController.cs                   ← File FIR, list, review
│   │   ├── SosController.cs                   ← Trigger emergency, logs, false alarm
│   │   ├── MapController.cs                   ← Map incidents, heatmap data, categories
│   │   ├── SmsController.cs                   ← Send SMS, bulk SMS, status
│   │   ├── VoiceCallController.cs             ← Start/end calls, transcript, status
│   │   └── ElevenLabsWebhookController.cs     ← ElevenLabs voice agent webhook
│   │
│   ├── Services/
│   │   ├── IAuthService.cs                    ← Auth interface
│   │   ├── AuthService.cs                     ← Identity-based auth, JWT generation
│   │   ├── IIncidentService.cs                ← Incident interface
│   │   ├── IncidentService.cs                 ← Full incident CRUD, maps, stats
│   │   ├── IAlertService.cs                   ← Alert interface
│   │   ├── AlertService.cs                    ← Alert CRUD, proximity filtering
│   │   ├── IFirService.cs                     ← FIR interface
│   │   ├── FirService.cs                      ← FIR CRUD, review, status management
│   │   ├── ISosService.cs                     ← SOS interface
│   │   ├── SosService.cs                      ← Emergency trigger, call logs
│   │   ├── ISpeechToText.cs                   ← STT interface
│   │   ├── MockSttService.cs                  ← Speech-to-text (mock)
│   │   ├── ITextToSpeech.cs                   ← TTS interface
│   │   ├── MockTtsService.cs                  ← Text-to-speech (mock)
│   │   ├── ILanguageModel.cs                  ← LLM interface
│   │   ├── GroqLlmService.cs                  ← Groq API LLM integration
│   │   ├── MockLlmService.cs                  ← Fallback keyword-based LLM
│   │   ├── IVoiceActivityDetector.cs          ← Voice activity detection interface
│   │   ├── IVoicePipeline.cs                  ← Voice pipeline interface
│   │   ├── VoicePipelineService.cs            ← STT → LLM → TTS orchestrator
│   │   ├── IVoiceCallService.cs               ← Voice call interface
│   │   ├── VoiceCallService.cs                ← Call session management, SignalR broadcast
│   │   ├── CallSession.cs                     ← Call session state record
│   │   ├── ISmsService.cs                     ← SMS interface
│   │   ├── MockSmsService.cs                  ← SMS sender (mock)
│   │   └── ToastService.cs                    ← Client-side toast notifications
│   │
│   ├── Hubs/
│   │   ├── IncidentHub.cs                     ← Real-time incident updates
│   │   ├── AlertHub.cs                        ← Real-time alert broadcasting
│   │   ├── MapHub.cs                          ← Real-time map location/incident events
│   │   └── CallHub.cs                         ← Real-time call monitoring
│   │
│   ├── Models/
│   │   ├── User.cs                            ← Identity user (Guid PK)
│   │   ├── Enums.cs                           ← All enum types
│   │   ├── Incident.cs                        ← Incident entity
│   │   ├── IncidentCategory.cs                ← Incident category entity
│   │   ├── FIRReport.cs                       ← FIR report entity
│   │   ├── Alert.cs                           ← Alert entity
│   │   ├── Authority.cs                       ← Authority profile entity
│   │   ├── AICallLog.cs                       ← AI call log entity
│   │   ├── Comment.cs                         ← Comment entity
│   │   ├── Notification.cs                    ← Notification entity
│   │   └── Response.cs                        ← Authority response entity
│   │
│   ├── DTOs/
│   │   ├── LoginDto.cs                        ← Login request
│   │   ├── RegisterDto.cs                     ← Registration request
│   │   ├── AuthResponseDto.cs                 ← Auth response (token, user)
│   │   ├── RefreshTokenDto.cs                 ← Token refresh request
│   │   ├── UserDto.cs                         ← User data transfer
│   │   ├── IncidentDtos.cs                    ← All incident DTOs
│   │   ├── AlertDtos.cs                       ← All alert DTOs
│   │   ├── FirDtos.cs                         ← All FIR DTOs
│   │   ├── SosDtos.cs                         ← SOS emergency DTOs
│   │   ├── VoiceCallDtos.cs                   ← Voice call DTOs
│   │   └── ElevenLabsWebhookDto.cs            ← ElevenLabs webhook payload
│   │
│   ├── Data/
│   │   ├── SafeZoneDbContext.cs               ← EF Core context (12 DbSets, indexes)
│   │   └── SeedData.cs                        ← Roles, categories, test users, sample data
│   │
│   ├── Migrations/
│   │   ├── 20260509203245_InitialCreate.cs     ← Initial database migration
│   │   ├── 20260509203245_InitialCreate.Designer.cs
│   │   └── SafeZoneDbContextModelSnapshot.cs   ← Current model snapshot
│   │
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs      ← Global exception handler
│   │
│   ├── Helpers/
│   │   └── GeoHelper.cs                        ← Haversine distance, coordinate validation
│   │
│   └── wwwroot/
│       ├── css/
│       │   └── global.css                      ← ★ 4100+ lines: tactical design system
│       └── js/
│           ├── map.js                           ← Leaflet map helper (370 lines)
│           └── index-effects.js                 ← Three.js 3D hero (289 lines)
│
└── SafeZone.Client/                             ← ★ Legacy static HTML client (pre-migration)
    ├── index.html
    ├── login.html / register.html
    ├── authority/dashboard.html
    ├── user/dashboard.html
    ├── css/global.css                           ← Client-side CSS (505 lines)
    └── js/
        ├── api.js                               ← API client wrapper
        ├── auth.js                              ← Auth state manager (localStorage)
        ├── signalr-client.js                    ← SignalR client manager
        └── toast.js                             ← Client toast notifications
```

---

## Database Design

### Entity Relationship Overview

```
User (IdentityUser<Guid>)
 ├── AuthorityProfile (1:1)
 ├── ReportedIncidents (1:N)
 ├── Comments (1:N)
 ├── Notifications (1:N)
 └── FIRReports (1:N)

Incident
 ├── Category (N:1)
 ├── Reporter (N:1 → User)
 ├── AssignedAuthority (N:1 → User)
 ├── FIR (1:1)
 ├── Comments (1:N)
 ├── Responses (1:N)
 └── AICallLog (1:1)

FIRReport
 ├── Incident (1:1)
 ├── Reporter (N:1 → User)
 └── ReviewedByAuthority (N:1 → User)

Alert
 └── IssuedByAuthority (N:1 → User)

AICallLog
 ├── Incident (N:1)
 └── TriggeredByUser (N:1 → User)
```

### Enumerations

| Enum | Values |
|------|--------|
| `UserRole` | Resident, Authority, SuperAdmin |
| `IncidentStatus` | Pending, Assigned, InProgress, Resolved, Closed |
| `SeverityLevel` | Low, Medium, High, Critical |
| `FIRStatus` | Submitted, UnderReview, Accepted, Rejected, Investigating, Closed |
| `AuthorityType` | Police, Ambulance, FireBrigade, TrafficPolice |
| `CallStatus` | Initiated, Ringing, Answered, Completed, Failed, NoAnswer, Cancelled |
| `AlertType` | Emergency, Warning, Info, WeatherAlert, CurfewNotice |
| `AlertScope` | Citywide, Radius, SpecificArea, AllAuthorities |

### Seeded Data

- **3 Roles:** Resident, Authority, Admin, SuperAdmin
- **15 Incident Categories:** Theft, Assault, Sexual Harassment, Robbery, Accident, Fire, Medical Emergency, Missing Person, Suspicious Activity, Noise Complaint, Vandalism, Traffic Violation, Curfew Violation, Shooting, Other
- **3 Test Users:** SuperAdmin, Authority Officer (Inspector Ahmed Khan), Resident (Ali Hassan)
- **5 Sample Incidents:** Car theft, traffic accident, suspicious loitering, gunshots, street robbery

---

## REST API Reference

### Base URL: `http://localhost:5002/api`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | No | Register new user |
| POST | `/auth/login` | No | Login, returns JWT + cookie |
| POST | `/auth/refresh` | Yes | Refresh JWT token |
| POST | `/auth/logout` | Yes | Logout, clear auth |
| GET | `/auth/me` | Yes | Current user profile |
| | | | |
| GET | `/incident/categories` | No | List all categories |
| POST | `/incident` | Yes | Create incident |
| GET | `/incident` | Authority+ | List all incidents (with filters) |
| GET | `/incident/my` | Yes | List user's incidents |
| GET | `/incident/{id}` | Yes | Get incident by ID |
| PUT | `/incident/{id}` | Yes | Update incident |
| PUT | `/incident/{id}/status` | Authority+ | Update incident status |
| GET | `/incident/stats` | Authority+ | Incident statistics |
| | | | |
| POST | `/alert` | Authority+ | Create alert |
| GET | `/alert/active` | No | List active alerts |
| GET | `/alert/nearby?lat=&lng=` | No | Alerts near location |
| GET | `/alert` | Authority+ | List all alerts |
| GET | `/alert/{id}` | Yes | Get alert by ID |
| PUT | `/alert/{id}/deactivate` | Authority+ | Deactivate alert |
| | | | |
| POST | `/fir` | Resident | File new FIR |
| GET | `/fir` | Authority+ | List all FIRs |
| GET | `/fir/my` | Resident | List user's FIRs |
| GET | `/fir/{id}` | Yes | Get FIR by ID |
| PUT | `/fir/{id}/review` | Authority+ | Review FIR (accept/reject/investigate) |
| | | | |
| POST | `/sos` | Resident | Trigger emergency SOS |
| GET | `/sos/my-logs` | Resident | My SOS call history |
| GET | `/sos/logs` | Authority+ | All SOS call logs |
| GET | `/sos/logs/{id}` | Yes | Get call log detail |
| PUT | `/sos/logs/{id}/false-alarm` | Authority+ | Mark as false alarm |
| GET | `/sos/status` | Yes | Check SOS system status |
| | | | |
| GET | `/map/incidents?swLat=&swLng=&neLat=&neLng=` | No | Map incidents in bounds |
| GET | `/map/heatmap?days=7` | No | Heatmap data points |
| GET | `/map/categories` | No | Map-specific categories |
| | | | |
| POST | `/sms/send` | Authority+ | Send single SMS |
| POST | `/sms/bulk` | Authority+ | Send bulk SMS |
| GET | `/sms/status` | Yes | SMS system status |
| | | | |
| POST | `/voicecall/start` | Authority+ | Start outbound voice call |
| GET | `/voicecall/active` | Authority+ | List active calls |
| GET | `/voicecall/{callId}` | Yes | Get call details |
| GET | `/voicecall/{callId}/transcript` | Yes | Get call transcript |
| POST | `/voicecall/{callId}/end` | Authority+ | End active call |
| GET | `/voicecall/status` | Yes | Voice system status |
| | | | |
| POST | `/elevenlabswebhook` | No | ElevenLabs voice agent webhook |

---

## SignalR Real-Time Hubs

| Hub | Route | Purpose | Key Events |
|-----|-------|---------|------------|
| **IncidentHub** | `/hubs/incidents` | Incident collaboration | `ReceiveComment`, `ReceiveStatusUpdate` |
| **AlertHub** | `/hubs/alerts` | Alert broadcasting | `ReceiveAlert`, `EmergencyCallRequested` |
| **MapHub** | `/hubs/map` | Live map updates | `NewIncidentReported`, `UserLocationUpdated`, `IncidentResolved` |
| **CallHub** | `/hubs/calls` | Voice call monitoring | `CallStatusUpdated`, `TranscriptSegment`, `AgentSpeaking`, `NewCallStarted`, `CallEnded` |

---

## Authentication & Authorization

### Flow

```
User Login
    │
    ├── Cookie Auth (Application → CookieAuth)
    │   └── For server-side Blazor pages
    │
    ├── JWT Bearer Token
    │   └── For REST API calls (15 min expiry)
    │
    └── Google OAuth
        └── External login callback → creates/links account
```

### Roles & Access

| Role | Access Level |
|------|-------------|
| **Resident** | Report incidents, file FIRs, trigger SOS, view own data, weather map |
| **Authority** | All Resident access + manage incidents, review FIRs, dispatch map, broadcast alerts, AI agent, voice calls |
| **Admin** | Same as Authority (reserved role) |
| **SuperAdmin** | Full admin + user management, system settings |

### Test Credentials

| Role | Phone | Password |
|------|-------|----------|
| SuperAdmin | +92511234567 | Admin123! |
| Authority | +92511112233 | Officer123! |
| Resident | +923001234567 | User123! |

---

## Voice AI Pipeline

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  STT          │ →  │  LLM          │ →  │  TTS          │
│  Speech→Text  │    │  Text→Response│    │  Response→Audio│
│              │    │              │    │              │
│  MockSttSvc  │    │  GroqLlmSvc   │    │  MockTtsSvc   │
│  (11Labs alt)│    │  (Mock fallbk)│    │  (11Labs alt) │
└──────────────┘    └──────────────┘    └──────────────┘
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
                  VoicePipelineService
                  (orchestrator)
                           │
                  VoiceCallService
                  (session management)
                           │
                  ┌────────┴────────┐
                  │                 │
            CallHub (SignalR)  ElevenLabs Webhook
            (real-time)        (voice agent integration)
```

### Pipeline Services

| Interface | Implementation | Status | Description |
|-----------|---------------|--------|-------------|
| `ISpeechToText` | `MockSttService` | ✅ Done | Returns mock emergency transcripts |
| `ILanguageModel` | `GroqLlmService` | ✅ Done | Groq API with fallback to keyword matching |
| `ITextToSpeech` | `MockTtsService` | ✅ Done | Generates WAV audio with metadata |
| `IVoiceActivityDetector` | _(Not implemented)_ | ⬜ Pending | Voice activity detection |
| `IVoicePipeline` | `VoicePipelineService` | ✅ Done | STT → LLM → TTS orchestrator |
| `IVoiceCallService` | `VoiceCallService` | ✅ Done | Call session state, SignalR broadcast |
| `CallSession` | Record type | ✅ Done | Call ID, direction, history, transcript |

---

## External Integrations

### Implemented

| Service | Purpose | Status |
|---------|---------|--------|
| **Groq API** | LLM for AI emergency script generation | ✅ Done |
| **Open-Meteo API** | Live weather data (temperature, humidity, rain, UV, wind) | ✅ Done |
| **ElevenLabs Convai** | Voice agent widget on dashboard | ✅ Done |
| **ElevenLabs Webhook** | Receives voice agent callbacks, creates incidents | ✅ Done |
| **Google OAuth** | Social login | ✅ Done |
| **Leaflet.js** | Interactive maps with incident markers | ✅ Done |
| **Leaflet-Heat** | Heatmap layer for incident density | ✅ Done |
| **Three.js** | 3D safety visualization on landing page | ✅ Done |
| **ONNX Runtime** | ML runtime (configured, not yet utilized) | ⬜ Pending |

### Planned / In Progress

| Service | Purpose | Status |
|---------|---------|--------|
| **Gmail API** | Email notifications for alerts and FIR status updates | ⬜ Not Started |
| **Slack Webhooks** | Authority notification channel for critical incidents | ⬜ Not Started |
| **Twilio Voice** | Real outbound emergency calls (mock mode currently) | ⬜ Not Started |

---

## Team Division — Who Did What

### 🎨 **Aqib — Frontend & UI/UX**

All user-facing interface, styling, animations, and client-side interactivity.

#### Layouts (3 files)
| File | Description |
|------|-------------|
| `Components/Layout/MainLayout.razor` | Public layout: navbar with SafeZone logo, Sign In/Get Started, renders body |
| `Components/Layout/LoginLayout.razor` | Auth layout: dark ambient glow background for login/register |
| `Components/Layout/DashboardLayout.razor` | Authenticated layout: collapsible sidebar, topbar with user info, avatar, notifications, logout, 15-min session auto-logout timer, status bar |

#### Public Pages (5 files)
| File | Route | Description |
|------|-------|-------------|
| `Pages/Index.razor` | `/` | Landing page: 3D Three.js molecular hero, feature cards (Map/SOS/FIR/Real-time/Authority/Security), tech stack showcase, CTA buttons, counter animation |
| `Pages/Login.razor` | `/login` | Terminal-style login: "Secure Access Terminal", bottom-border inputs with cyan glow, "AUTHENTICATE" button with neon green + animated conic border, Google OAuth, test credentials |
| `Pages/Register.razor` | `/register` | Terminal-style registration: "Initialize Account", role selection cards (Resident/Authority), form validation |
| `Pages/NotFound.razor` | `/not-found` | 404 error page with glass styling |
| `Pages/AccessDenied.razor` | `/access-denied` | 403 error page with glass styling |

#### User Pages (10 files)
| File | Route | Description |
|------|-------|-------------|
| `Pages/User/Dashboard.razor` | `/user/dashboard` | Resident dashboard: MetricCards (Reports/Active/SOS/Weather), Quick Actions grid (Report/SOS/FIR/MyReports/Alerts/WeatherMap), Live Map preview (Leaflet.js), Weather card (Open-Meteo API), Safety Overview, Recent Incidents list |
| `Pages/User/ReportIncident.razor` | `/user/report-incident` | 4-step incident wizard: ① Type & Severity (category grid + severity picker), ② Location (map pin + address), ③ Details (title/description/photos/anonymous toggle), ④ Review & Submit |
| `Pages/User/MyIncidents.razor` | `/user/my-incidents` | Incident history: filter by status (All/Pending/Assigned/InProgress/Resolved/Closed), detail modal with full incident info, pagination |
| `Pages/User/Sos.razor` | `/user/sos` | SOS Emergency: pulse-animated SOS button, emergency type picker (Police🚔/Ambulance🚑/Fire🚒/Traffic🚦), live location map, 3-second countdown overlay with dramatic red numbers, calling overlay with ripple rings + live timer + progress steps, result overlay with green gradient border |
| `Pages/User/FileFir.razor` | `/user/file-fir` | 4-step FIR wizard: Complainant details → Incident narrative → Accused info → Declaration & Submit |
| `Pages/User/MyFirs.razor` | `/user/my-firs` | Filed FIR list with status filtering, detail modal |
| `Pages/User/WeatherMap.razor` | `/user/weather-map` | Multi-layer map: Rainfall/Temperature/Pollution/Incidents/Combined layers, Open-Meteo data, heat points generation, weather alerts, color-coded legend |
| `Pages/User/Notifications.razor` | `/user/notifications` | Notification center with type filtering, mark-read, detail view |
| `Pages/User/Profile.razor` | `/user/profile` | View/edit full name, phone, email, join date |
| `Pages/User/Settings.razor` | `/user/settings` | Toggle push notifications, anonymous reporting, proximity radius slider |

#### Authority Pages (9 files)
| File | Route | Description |
|------|-------|-------------|
| `Pages/Authority/AuthorityBoard.razor` | `/authority/board` | Command Center: 4 MetricCards (Total/Active/Resolved/Pending FIRs), Quick Actions grid (Dispatch Map/Field Reports/FIR Management/Broadcast Alert), Recent Incidents tactical table, Active Alerts with severity-coded left borders |
| `Pages/Authority/KanbanBoard.razor` | `/authority/kanban` | Incident Kanban: 5 columns (Pending→Assigned→InProgress→Resolved→Closed), color-coded top accent bars, incident cards with severity left-border indicators, click-to-move modal with status transitions |
| `Pages/Authority/DispatchMap.razor` | `/authority/dispatch` | Live dispatch: full-page Leaflet map with incident markers, real-time updates via MapHub, status/severity filters, auto-refresh toggle, incident sidebar with detail cards |
| `Pages/Authority/FieldReports.razor` | `/authority/field-reports` | Incident management table: filtering, search, bulk actions, severity/status visualization, export |
| `Pages/Authority/FIRManagement.razor` | `/authority/fir-management` | FIR review: status counts, filterable table, detail modal, Accept/Reject/Investigate actions |
| `Pages/Authority/SosLogs.razor` | `/authority/sos-logs` | Emergency call logs: status filtering, full call detail view |
| `Pages/Authority/AuthoritySettings.razor` | `/authority/settings` | Profile management, broadcast alert form (citywide/radius targeting), SMS test sender, Google OAuth config |
| `Pages/Authority/AiAgent.razor` | `/authority/ai-agent` | AI chat: Groq LLM-powered assistant, collapsible system prompt, chat bubble UI, markdown rendering |
| `Pages/Authority/UserManagement.razor` | `/authority/user-management` | SuperAdmin only: user list with role filtering, activate/deactivate toggle, role change, search by phone/name |

#### Shared Components (18 files)
| Component | Description |
|-----------|-------------|
| `AppSidebar.razor` | Role-based navigation sidebar with icon labels, active state highlighting |
| `Badge.razor` | Inline status/type badge chip |
| `Breadcrumbs.razor` | Breadcrumb navigation trail |
| `ConfirmDialog.razor` | Modal confirmation dialog |
| `EmptyState.razor` | Empty state with icon, title, message, optional action button |
| `GlassCard.razor` | Glassmorphism card wrapper with title, header, action slot |
| `IncidentCard.razor` | Incident card with severity-coded left border, hover glow |
| `LoadingSkeleton.razor` | Skeleton loading: card/text/page/stats variants |
| `LoadingSpinner.razor` | Animated spinner |
| `MetricCard.razor` | Stat card: label, value, circular icon with glow, accent color, change indicator, staggered entrance animation |
| `PageHeader.razor` | Page title with subtitle and optional action slot |
| `RedirectToLogin.razor` | Auth redirect logic handler |
| `SearchToolbar.razor` | Search input with icon |
| `SectionContainer.razor` | Section wrapper with title header |
| `SeverityChip.razor` | Severity level indicator chip |
| `StatCard.razor` | Simplified stat display variant |
| `StatusBar.razor` | Live clock (1s timer), "SYSTEM ONLINE" pulsing indicator, user role display |
| `StatusChip.razor` | Status indicator chip |
| `Toast.razor` | Toast notification system: 4 types (success/warning/error/info), color-coded left border glow, progress bar animation, slide-in from right, auto-dismiss 4s, max 5 visible |

#### CSS & JavaScript (3 files)
| File | Lines | Description |
|------|-------|-------------|
| `wwwroot/css/global.css` | 4100+ | Complete tactical design system: CSS custom properties (colors, shadows, radii), tactical color palette (neon-green/electric-cyan/alert-red/warning-amber/command-purple with dim/glow variants), 8 keyframe animations (grid-pan, pulse-glow, shimmer, card-enter, page-enter, border-rotate, live-dot-pulse, ticker-scroll), scan lines overlay, animated dot-grid background, glassmorphism system, component styles (buttons, badges, cards, tables, modals, forms), layout styles (navbar, sidebar, topbar, content), page-specific styles (SOS countdown/calling/result, Kanban board, weather map, login/register terminals, notifications) |
| `wwwroot/js/map.js` | 370 | `window.safezoneMap` namespace: init map with dark CartoDB tiles, add incident markers with severity-colored icons, add/remove heatmap layers (heatPoints format), clear markers, fit bounds to incidents, invalidate size, dispose maps. Supports multiple map instances. |
| `wwwroot/js/index-effects.js` | 289 | Three.js 3D hero: icosahedron-based molecular structure with neon-green edges and glowing sphere nodes, auto-rotation on Y-axis, mouse-interactive orbit, responsive resize handler. Initialized via `initHeroEffects()` called from Index.razor. |

---

### ⚙️ **Talha — Operations & Voice Agent Integration + External APIs**

All voice AI pipeline, emergency operations, real-time communication, and external API integrations.

#### Voice AI Pipeline (9 files)
| File | Description |
|------|-------------|
| `Services/ISpeechToText.cs` | STT interface: `TranscribeAsync`, `TranscribeStreamAsync` |
| `Services/MockSttService.cs` | Mock STT implementation: returns curated emergency transcripts from predefined array |
| `Services/ITextToSpeech.cs` | TTS interface: `SynthesizeAsync` |
| `Services/MockTtsService.cs` | Mock TTS implementation: generates silent WAV audio with metadata headers |
| `Services/ILanguageModel.cs` | LLM interface: `GenerateResponseAsync` |
| `Services/GroqLlmService.cs` | Primary LLM via Groq API (chat completions). Falls back to `MockLlmService` keyword-based matching if API key not configured |
| `Services/IVoiceActivityDetector.cs` | Voice activity detection interface (pending implementation) |
| `Services/IVoicePipeline.cs` | Voice pipeline interface: `ProcessTurnAsync`, `SynthesizeTextAsync`, `TranscribeAudioAsync` |
| `Services/VoicePipelineService.cs` | Pipeline orchestrator: STT → LLM → TTS. Coordinates the full voice processing flow |

#### Voice Call System (3 files)
| File | Description |
|------|-------------|
| `Services/IVoiceCallService.cs` | Voice call interface: start/end calls, get active calls, get transcripts |
| `Services/VoiceCallService.cs` | Call session management: `ConcurrentDictionary<Guid, CallSession>` for active calls, broadcasts status/transcript/agent-speaking events via SignalR `CallHub`, notifies authorities of new calls |
| `Services/CallSession.cs` | Call state record: CallId, RemoteNumber, Direction (Inbound/Outbound), Status, ConversationHistory, Transcript, timestamps, SystemPrompt, IsMock |

#### SOS Emergency System (2 files)
| File | Description |
|------|-------------|
| `Services/ISosService.cs` | SOS interface: trigger emergency, get call logs, mark false alarm |
| `Services/SosService.cs` | Emergency trigger: creates Incident + AICallLog + optional mock voice call. Hardcoded emergency numbers (Police=15, Ambulance=115, Fire=16, Traffic=1915). Generates detailed AI emergency scripts describing location, situation, and required response |

#### SMS Service (2 files)
| File | Description |
|------|-------------|
| `Services/ISmsService.cs` | SMS interface: `SendSmsAsync`, `SendBulkSmsAsync` |
| `Services/MockSmsService.cs` | Mock SMS implementation: simulates SMS sending with detailed console logging |

#### Real-Time Hubs (4 files)
| File | Description |
|------|-------------|
| `Hubs/IncidentHub.cs` | Incident collaboration: `JoinIncidentRoom`, `LeaveIncidentRoom`, `SendComment`, `SendStatusUpdate` |
| `Hubs/AlertHub.cs` | Alert broadcasting: `JoinAuthorityGroup`, `JoinLocationArea`, `BroadcastAlert`, `SendEmergencyCallRequest` |
| `Hubs/MapHub.cs` | Live map updates: `UpdateLocation`, `ReportNewIncident`, `IncidentResolved` |
| `Hubs/CallHub.cs` | Voice call monitoring: `JoinCallMonitoring`, call status/transcript/agent-speaking events, new call notifications |

#### Voice & Emergency Controllers (3 files)
| File | Description |
|------|-------------|
| `Controllers/SosController.cs` | POST trigger emergency, GET logs, PUT false-alarm, GET status |
| `Controllers/VoiceCallController.cs` | POST start/end call, GET active calls, GET transcript, GET status |
| `Controllers/ElevenLabsWebhookController.cs` | Receives ElevenLabs voice agent webhook payloads, parses dynamic variables, creates incidents, broadcasts via MapHub |

#### Voice & Emergency DTOs (3 files)
| File | Description |
|------|-------------|
| `DTOs/SosDtos.cs` | `TriggerSosDto`, `SosResponseDto`, `SosCallLogDto` |
| `DTOs/VoiceCallDtos.cs` | `StartCallDto`, `CallResponseDto`, `TranscriptSegmentDto` |
| `DTOs/ElevenLabsWebhookDto.cs` | `ElevenLabsWebhookPayload` (+ nested classes), `ElevenLabsWebhookResponse` |

#### External API Integrations (Done)
| Integration | Implementation | File |
|-------------|---------------|------|
| **Groq LLM API** | `GroqLlmService.cs` chat completions endpoint for AI emergency scripts | `Services/GroqLlmService.cs` |
| **Open-Meteo Weather** | `Dashboard.razor` HTTP fetch: temperature, humidity, wind, rain, UV, weather code | `Pages/User/Dashboard.razor` (code block) |
| **ElevenLabs Convai** | Widget embedded in `_Host.cshtml` for voice agent on frontend | `Pages/_Host.cshtml` |
| **ElevenLabs Webhook** | `ElevenLabsWebhookController.cs` receives agent callbacks | `Controllers/ElevenLabsWebhookController.cs` |

#### Pending External Integrations
| Integration | Purpose | Status |
|-------------|---------|--------|
| **Gmail API** | Email notifications: FIR status updates, incident alerts, SOS confirmations | ⬜ Not Started |
| **Slack Webhooks** | Authority notification channel: critical incidents, SOS alerts, daily summaries | ⬜ Not Started |
| **Twilio Voice (Live)** | Replace mock voice calls with real outbound emergency calling | ⬜ Not Started |
| **WhatsApp Business API** | Emergency notifications via WhatsApp | ⬜ Not Started |

#### SignalR Hub Registration (in Program.cs)
All 4 hubs mapped to routes in `Program.cs`:
```csharp
app.MapHub<IncidentHub>("/hubs/incidents");
app.MapHub<AlertHub>("/hubs/alerts");
app.MapHub<MapHub>("/hubs/map");
app.MapHub<CallHub>("/hubs/calls");
```

---

### 🗄️ **Siddique — Backend & Database**

All data layer, business logic services, authentication/authorization, REST API controllers, and application infrastructure.

#### Database Layer (8 files)
| File | Description |
|------|-------------|
| `Data/SafeZoneDbContext.cs` | EF Core context: `IdentityDbContext<User, IdentityRole<Guid>, Guid>`. 12 DbSets (Incidents, Authorities, IncidentCategories, FIRReports, AICallLogs, Alerts, Notifications, Comments, Responses). Configures enum→string conversions, renames Identity tables, extensive indexes on status/severity/coordinates/timestamps, unique constraints |
| `Data/SeedData.cs` | Database seeder: 3 roles (Resident/Authority/SuperAdmin), 15 incident categories with icons/colors, 3 test users with hashed passwords, 5 sample incidents with Islamabad coordinates |
| `Migrations/20260509203245_InitialCreate.cs` | Initial EF migration: creates all tables, relationships, indexes, constraints |
| `Migrations/20260509203245_InitialCreate.Designer.cs` | Migration designer file |
| `Migrations/SafeZoneDbContextModelSnapshot.cs` | Current EF model snapshot |

#### Domain Models (10 files)
| File | Entity | Key Properties |
|------|--------|----------------|
| `Models/User.cs` | User (extends `IdentityUser<Guid>`) | FullName, Role (UserRole), PhoneHash, LastKnownLat/Lng, ProximityRadiusKm, IsAnonymous, PushNotificationsEnabled, CreatedAt, LastActiveAt |
| `Models/Enums.cs` | — | UserRole, IncidentStatus, SeverityLevel, FIRStatus, AuthorityType, CallStatus, AlertType, AlertScope |
| `Models/Incident.cs` | Incident | IncidentId (Guid), IncidentNumber (auto-gen), CategoryId, ReporterId, Lat/Lng, Title, Description, Status, Severity, IsAnonymous, EvidenceUrls, ReportedAt, ResolvedAt, AIGeneratedSummary |
| `Models/IncidentCategory.cs` | IncidentCategory | CategoryId, Name (unique), Icon, Color, Description |
| `Models/FIRReport.cs` | FIRReport | FIRId, FIRNumber (auto-gen), IncidentId, ReporterId, Complainant/Accused details, IncidentNarrative, Status, DigitalSignature, PDFUrl |
| `Models/Alert.cs` | Alert | AlertId, IssuedByAuthorityId, Title, Message, Type, Scope, CenterLat/Lng, RadiusKm, IssuedAt, ExpiresAt |
| `Models/Authority.cs` | Authority | AuthId, UserId (1:1), UnitName, BadgeNumber, JurisdictionGeoJson, ContactInfo, IsOnDuty, Type, Rank, Department |
| `Models/AICallLog.cs` | AICallLog | LogId, IncidentId, CallType, TwilioCallSid (unique), AIScript, Status (CallStatus), DurationSeconds, TranscriptUrl, IsFalseAlarm |
| `Models/Comment.cs` | Comment | IncidentId + UserId FKs, Message, IsOfficialUpdate, CreatedAt |
| `Models/Notification.cs` | Notification | UserId FK, Type, Title, Message, IsRead, CreatedAt |
| `Models/Response.cs` | Response | IncidentId + AuthorityId FKs, Notes, RespondedAt, StatusUpdate |

#### DTOs (8 files)
| File | Classes |
|------|---------|
| `DTOs/LoginDto.cs` | `LoginDto` (PhoneNumber, Password) |
| `DTOs/RegisterDto.cs` | `RegisterDto` (PhoneNumber, Password, ConfirmPassword, FullName, Role) |
| `DTOs/AuthResponseDto.cs` | `AuthResponseDto` (Success, Message, Token, RefreshToken, ExpiresAt, UserDto) |
| `DTOs/RefreshTokenDto.cs` | `RefreshTokenDto` |
| `DTOs/UserDto.cs` | `UserDto` (full profile, excludes sensitive data) |
| `DTOs/IncidentDtos.cs` | `CreateIncidentDto`, `UpdateIncidentDto`, `IncidentResponseDto`, `IncidentListDto`, `MapIncidentDto`, `HeatmapPointDto`, `CategoryDto` |
| `DTOs/AlertDtos.cs` | `CreateAlertDto`, `AlertResponseDto`, `AlertListDto` |
| `DTOs/FirDtos.cs` | `CreateFirDto`, `FirResponseDto`, `FirListDto`, `ReviewFirDto` |

#### Core Business Services (8 files)
| File | Description |
|------|-------------|
| `Services/IAuthService.cs` / `AuthService.cs` | Identity-based auth: RegisterAsync, LoginAsync, RefreshTokenAsync, LogoutAsync, GetUserByIdAsync, GenerateJwtTokenAsync (JWT with claims: sub, name, phone, role), GenerateRefreshToken |
| `Services/IIncidentService.cs` / `IncidentService.cs` | Incident CRUD: CreateIncident (auto-generates INC-yyyyMMdd-NNNN numbers), GetById, GetMyIncidents, GetAllIncidents (filtered by status/severity/category), UpdateIncident, UpdateStatus, AssignAuthority, GetIncidentsForMap (geo-bounds), GetHeatmapData, GetCategories, GetStats (counts by status and severity) |
| `Services/IAlertService.cs` / `AlertService.cs` | Alert CRUD: CreateAlert, GetById, GetActiveAlerts, GetAllAlerts, DeactivateAlert, GetAlertsForLocation (proximity-based using GeoHelper) |
| `Services/IFirService.cs` / `FirService.cs` | FIR CRUD: CreateFir (auto-generates FIR-yyyyMMdd-NNNN numbers), GetById, GetMyFirs, GetAllFirs, ReviewFir (accept/reject/investigate), GetFirsByStatus |

#### REST API Controllers (6 files)
| File | Endpoints |
|------|-----------|
| `Controllers/AuthController.cs` | POST register, POST login, POST refresh, POST logout, GET me |
| `Controllers/IncidentController.cs` | GET categories (anon), POST create, GET all (Authority+), GET my, GET {id}, PUT update, PUT {id}/status (Authority+), GET stats (Authority+) |
| `Controllers/AlertController.cs` | POST create (Authority+), GET active (anon), GET nearby (anon), GET all (Authority+), GET {id}, PUT {id}/deactivate (Authority+) |
| `Controllers/FirController.cs` | POST create (Resident), GET all (Authority+), GET my, GET {id} (owner or Authority+), PUT {id}/review (Authority+) |
| `Controllers/MapController.cs` | GET incidents (with geo-bounds filter), GET heatmap (days filter), GET categories |
| `Controllers/SmsController.cs` | POST send (Authority+), POST bulk (Authority+), GET status |

#### Infrastructure (3 files)
| File | Description |
|------|-------------|
| `Middleware/ExceptionHandlingMiddleware.cs` | Global exception handler: catches exceptions, returns JSON error responses (401/404/500) |
| `Helpers/GeoHelper.cs` | Geo utilities: Haversine distance calculation (`CalculateDistanceKm`), coordinate validation, bounds calculation |
| `Program.cs` | **Application entry point (525 lines):** SQLite + EF Core config, ASP.NET Core Identity (User with Guid keys, RoleManager), Authentication (Cookie Application + Cookie External + JWT Bearer + Google OAuth), Authorization with role-based policies, Blazor Server with Razor Pages, SignalR hubs (4 mapped), DI registration (all services, voice pipeline singletons, toast service as scoped), CORS policy, Swagger/OpenAPI, seed data initialization, custom Blazor auth endpoints (`/blazor-login`, `/blazor-register`, `/blazor-logout`, `/clear-auth`), Google OAuth endpoints (`/external-login`, `/external-login-callback`), cache control headers, `BuildSafeZonePrincipalAsync()` claims construction helper |

---

## Feature Status

### ✅ Completed

| Category | Feature | Files |
|----------|---------|-------|
| **Auth** | Login with phone + password | Login.razor, AuthController.cs, AuthService.cs |
| **Auth** | Registration with role selection | Register.razor, AuthController.cs |
| **Auth** | Google OAuth social login | Program.cs, external-login endpoints |
| **Auth** | JWT token generation + validation | AuthService.cs, Program.cs |
| **Auth** | Cookie authentication for Blazor Server | Program.cs |
| **User** | Role-based dashboard (Resident) | Dashboard.razor |
| **User** | 4-step incident reporting wizard | ReportIncident.razor |
| **User** | Incident history + detail modal | MyIncidents.razor |
| **User** | SOS emergency with countdown + animated calling | Sos.razor, SosService.cs |
| **User** | FIR filing wizard (4 steps) | FileFir.razor |
| **User** | Filed FIRs list + detail | MyFirs.razor |
| **User** | Multi-layer weather heatmap | WeatherMap.razor |
| **User** | Notification center | Notifications.razor |
| **User** | Profile management | Profile.razor |
| **User** | Settings (notifications, privacy, proximity) | Settings.razor |
| **Authority** | Command Center dashboard | AuthorityBoard.razor |
| **Authority** | Kanban board (5 columns, move incidents) | KanbanBoard.razor |
| **Authority** | Live dispatch map | DispatchMap.razor |
| **Authority** | Field reports management | FieldReports.razor |
| **Authority** | FIR review system (accept/reject/investigate) | FIRManagement.razor |
| **Authority** | SOS call log viewer | SosLogs.razor |
| **Authority** | AI chat assistant (Groq LLM) | AiAgent.razor |
| **Authority** | Broadcast alerts (citywide/radius) | AuthoritySettings.razor, AlertService.cs |
| **Admin** | User management (activate/deactivate, role change) | UserManagement.razor |
| **Voice** | STT → LLM → TTS pipeline | VoicePipelineService.cs |
| **Voice** | Groq LLM integration | GroqLlmService.cs |
| **Voice** | Call session management | VoiceCallService.cs, CallSession.cs |
| **Voice** | SignalR real-time call monitoring | CallHub.cs |
| **Voice** | ElevenLabs webhook integration | ElevenLabsWebhookController.cs |
| **Voice** | Mock STT/TTS for development | MockSttService.cs, MockTtsService.cs |
| **Real-Time** | Incident Hub (comments, status updates) | IncidentHub.cs |
| **Real-Time** | Alert Hub (broadcasting, location-based) | AlertHub.cs |
| **Real-Time** | Map Hub (location, incident markers) | MapHub.cs |
| **Real-Time** | Call Hub (call monitoring, transcripts) | CallHub.cs |
| **API** | Full REST API with Swagger docs | All 9 controllers |
| **Maps** | Leaflet.js maps with incident markers | map.js, DispatchMap.razor |
| **Maps** | Heatmap layer for incident density | map.js, WeatherMap.razor |
| **Maps** | Open-Meteo weather integration | Dashboard.razor (code block) |
| **UI** | Tactical design system (4100+ lines CSS) | global.css |
| **UI** | Glassmorphism component library | GlassCard.razor, MetricCard.razor, etc. |
| **UI** | 8 keyframe animations | global.css (grid-pan, pulse-glow, etc.) |
| **UI** | Responsive sidebar + mobile menu | DashboardLayout.razor, global.css |
| **UI** | Toast notification system | Toast.razor, ToastService.cs |
| **UI** | Loading skeletons and spinners | LoadingSkeleton.razor, LoadingSpinner.razor |
| **UI** | 3D Three.js landing page hero | index-effects.js, Index.razor |
| **UI** | ElevenLabs Convai voice widget | _Host.cshtml |
| **DB** | SQLite database with 12 tables | SafeZoneDbContext.cs |
| **DB** | EF Core migrations | Migrations/ |
| **DB** | Seed data (roles, categories, test users, samples) | SeedData.cs |
| **DB** | Auto-generated incident/FIR numbers | IncidentService.cs, FirService.cs |
| **Infra** | Global exception handling middleware | ExceptionHandlingMiddleware.cs |
| **Infra** | Geo distance calculation (Haversine) | GeoHelper.cs |
| **Infra** | CORS configuration | Program.cs |

### ⬜ Pending / Not Started

| Category | Feature | Assigned To |
|----------|---------|-------------|
| **Voice** | Live Twilio outbound calling (replace mock) | Talha |
| **Voice** | Voice Activity Detection implementation | Talha |
| **Integrations** | Gmail API for email notifications | Talha |
| **Integrations** | Slack webhooks for authority alerts | Talha |
| **Integrations** | WhatsApp Business API | Talha |
| **UI** | More page redesigns with tactical theme | Aqib |
| **UI** | Accessibility (prefers-reduced-motion) | Aqib |
| **UI** | Performance audit (60fps target) | Aqib |
| **Backend** | Email service for password reset | Siddique |
| **Backend** | File upload service (Azure/S3 blob storage) | Siddique |
| **Backend** | PDF generation for FIR reports | Siddique |
| **Backend** | Analytics dashboard with charts | Siddique |
| **Backend** | Audit logging | Siddique |
| **Backend** | Rate limiting on API endpoints | Siddique |
| **Backend** | Refresh token persistence and rotation | Siddique |

---

## Setup & Running

### Prerequisites
- .NET 8.0 SDK
- Any modern browser

### Quick Start

```bash
# Navigate to server project
cd SafeZone.Server

# Restore packages
dotnet restore

# Run database migrations (auto-applied on first run)
dotnet run

# Open browser
# HTTP:  http://localhost:5002
# HTTPS: https://localhost:7026
# Swagger: http://localhost:5002/swagger
```

### Test Credentials

| Role | Phone | Password |
|------|-------|----------|
| SuperAdmin | `+92511234567` | `Admin123!` |
| Authority | `+92511112233` | `Officer123!` |
| Resident | `+923001234567` | `User123!` |

### Configuration

Edit `appsettings.Development.json` to configure:

```json
{
  "Groq": {
    "ApiKey": "your-groq-api-key",
    "Model": "llama-3.3-70b-versatile"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  }
}
```

---

## Key Achievements

1. **Full-stack Blazor Server application** with 24 pages, 18 shared components, 16+ services, 9 controllers, 4 SignalR hubs
2. **Tactical Command Center UI** — 4100+ line custom CSS design system with glassmorphism, neon glows, scan lines, 8 custom animations
3. **Voice AI Pipeline** — STT → Groq LLM → TTS architecture with ElevenLabs Convai integration and webhook handling
4. **SOS Emergency System** — One-tap emergency triggering with countdown, animated calling sequence, progress tracking, and result display
5. **Incident Kanban Board** — 5-column drag-and-drop (via click) incident management with color-coded severity and status indicators
6. **Multi-layer Weather Map** — Open-Meteo live data + Leaflet heatmap with Rainfall/Temperature/Pollution/Incident layers
7. **Real-Time Communication** — 4 SignalR hubs for incidents, alerts, maps, and voice call monitoring
8. **REST API** — Full Swagger-documented API with 40+ endpoints across 9 controllers
9. **Role-Based Access** — 3 user roles (Resident/Authority/SuperAdmin) with granular page and API authorization
10. **Triple Authentication** — Cookie (Blazor) + JWT Bearer (API) + Google OAuth (social login)

---

*Project for CS-284L Visual Programming Lab — Air University Islamabad — Spring 2026*
