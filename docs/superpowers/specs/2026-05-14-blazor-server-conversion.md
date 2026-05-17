# Blazor Server Conversion — SafeZone

## Goal
Convert SafeZone from static HTML/CSS/JS + ASP.NET Core Web API into a full Blazor Server application while keeping the visual UI pixel-identical.

## Architecture

```
Browser ←→ Blazor Server SignalR Circuit
                ↓
         AuthenticationStateProvider (cookie auth + Google OAuth)
                ↓
         C# Services (direct DI, no HTTP fetch calls)
                ↓
         IJSRuntime (only for Leaflet.js maps + Three.js 3D sphere)
```

- **Auth**: Cookie-based via `SignInManager` for Blazor pages. JWT Bearer kept for external API consumers. Google OAuth added as external login provider.
- **Service calls**: Blazor pages inject C# services directly (AuthService, IncidentService, etc.) — no HTTP fetch/API controllers needed for frontend operations.
- **JS interop**: Leaflet.js (map.js) and Three.js (3D sphere) kept as pure JS files, invoked via `IJSRuntime` from Blazor pages. Everything else converted to C#.
- **Real-time**: Blazor's built-in SignalR circuit replaces manual SignalR client connections. Dedicated hub connections remain for cross-user broadcasts (incidents, alerts, calls).
- **Toast notifications**: Custom Blazor component replaces toast.js.
- **Forms**: `EditForm` + `DataAnnotationsValidator` with identical visual output.
- **CSS**: `global.css` kept unchanged. Tailwind CSS kept via CDN in `_Host.cshtml`. Page-specific inline `<style>` blocks moved to `<style>` within each `.razor` file.

## Project Structure

```
SafeZone.Server/
├── Program.cs
├── appsettings.json
├── _Imports.razor
├── App.razor
├── _Host.cshtml
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor + .cs
│   │   └── LoginLayout.razor         # Minimal layout for login/register pages
│   ├── Shared/
│   │   ├── Toast.razor + .cs
│   │   ├── LoadingSpinner.razor
│   │   ├── StatCard.razor
│   │   ├── Badge.razor
│   │   ├── IncidentCard.razor
│   │   ├── FilterTag.razor
│   │   ├── ConfirmDialog.razor
│   │   └── StepIndicator.razor
│   └── Pages/
│       ├── Index.razor
│       ├── Login.razor + .cs
│       ├── Register.razor
│       ├── User/
│       │   ├── Dashboard.razor
│       │   ├── ReportIncident.razor
│       │   ├── Sos.razor
│       │   ├── FileFir.razor
│       │   ├── MyFirs.razor
│       │   ├── MyIncidents.razor
│       │   ├── Notifications.razor
│       │   └── WeatherMap.razor
│       └── Authority/
│           ├── Dashboard.razor
│           ├── Incidents.razor
│           ├── Kanban.razor
│           ├── Firs.razor
│           ├── SosLogs.razor
│           ├── BroadcastAlert.razor
│           └── AiAgent.razor
```

## Implementation Phases

### Phase 1 — Layout + Shared Components
- `_Imports.razor`, `App.razor`, `_Host.cshtml`
- `MainLayout.razor` with NavBar, ambient glow, `<Toast />`
- `LoginLayout.razor` (minimal, no nav)
- Shared components: Toast, LoadingSpinner, StatCard, Badge, FilterTag, ConfirmDialog, StepIndicator
- Remove old `wwwroot/*.html`, `wwwroot/js/auth.js`, `wwwroot/js/toast.js`, `wwwroot/js/signalr-client.js`, `wwwroot/js/api.js`, `wwwroot/js/geolocation.js`
- Keep `wwwroot/css/global.css`, `wwwroot/js/map.js`, `wwwroot/` images if any

### Phase 2 — Auth Pages
- `Login.razor` — username/password + Google OAuth button
- `Register.razor` — registration form
- Cookie auth setup in Program.cs + Google OAuth config
- Remove `login.html`, `register.html`

### Phase 3 — Resident Pages
- Dashboard, SOS, ReportIncident, FileFir, MyFirs, MyIncidents, Notifications, WeatherMap
- Each page converts inline JS logic to C# code-behind
- SOS countdown timer → C# `Timer` + `InvokeAsync(StateHasChanged)`
- Weather API → `HttpClient` call from C#
- Leaflet map → JS interop calls to `map.js`
- Remove `user/*.html`

### Phase 4 — Authority Pages
- Dashboard, Incidents, Kanban, Firs, SosLogs, BroadcastAlert, AiAgent
- Kanban drag-and-drop → JS interop (SortableJS or HTML5 drag) or Blazor re-render approach
- Remove `authority/*.html`

### Phase 5 — Index Page
- `Index.razor` — landing page with hero, feature cards, tech stack, CTA
- Three.js 3D sphere → JS interop to existing three.js code
- Remove `index.html`

## Auth Details
- `Program.cs`: Add `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)` + `AddCookie()` + `AddGoogle()` (Google OAuth)
- `Login.razor`: Email/password form + "Sign in with Google" button that redirects to `/signin-google`
- Role assignment on register: role selected via dropdown, assigned by Identity on creation
- `AuthenticationStateProvider` cascaded via `CascadingAuthenticationState` in `App.razor`
- Pages protected with `[Authorize(Roles = "Resident")]` etc.

## Files to Remove After Conversion
- All `wwwroot/*.html` files (17 files)
- `wwwroot/js/auth.js`
- `wwwroot/js/api.js`
- `wwwroot/js/toast.js`
- `wwwroot/js/signalr-client.js`
- `wwwroot/js/geolocation.js`

## Files to Keep
- `wwwroot/css/global.css` (unchanged)
- `wwwroot/js/map.js` (JS interop for Leaflet)
- Any images/assets in `wwwroot/`
