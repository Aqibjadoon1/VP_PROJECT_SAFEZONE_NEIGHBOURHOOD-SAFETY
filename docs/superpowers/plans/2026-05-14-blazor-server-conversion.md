# Blazor Server Conversion — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert SafeZone from static HTML/CSS/JS + ASP.NET Core Web API into a full Blazor Server application with Google OAuth, keeping the UI pixel-identical.

**Architecture:** Blazor Server SignalR circuit replaces JWT-based static frontend. Cookie auth + Google OAuth replace localStorage JWT management. C# services injected directly into pages replace HTTP fetch calls. Leaflet.js and Three.js kept as JS interop. All 17 HTML pages become .razor components.

**Tech Stack:** .NET 8 Blazor Server, ASP.NET Core Identity (cookie auth), Google OAuth, SignalR, Entity Framework Core 8, Tailwind CSS (CDN), Leaflet.js (JS interop), Three.js (JS interop)

---

### Phase 1: Layout + Shared Components (Foundation)

#### Task 1.1: Create _Imports.razor

**File:** Create: `SafeZone.Server/_Imports.razor`

```
@using System.Net.Http
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using SafeZone.Server
@using SafeZone.Server.Components
@using SafeZone.Server.Components.Layout
@using SafeZone.Server.Components.Shared
@using SafeZone.Server.Components.Pages
```

#### Task 1.2: Update Program.cs — add Blazor Server + Google OAuth + cookie auth

**Modify:** `SafeZone.Server/Program.cs`

Replace the entire file with:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SafeZone.Server.Data;
using SafeZone.Server.Models;
using SafeZone.Server.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SafeZone API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<SafeZoneDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<SafeZoneDbContext>()
.AddSignInManager()
.AddApiEndpoints();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
})
.AddIdentityCookies()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/signin-google";
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IFirService, FirService>();
builder.Services.AddScoped<ISosService, SosService>();

builder.Services.AddSingleton<ISpeechToText, MockSttService>();
builder.Services.AddSingleton<ILanguageModel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<GroqLlmService>>();
    var apiKey = config["Groq:ApiKey"];
    var modelName = config["Groq:ModelName"] ?? "llama-3.1-8b-instant";
    var endpoint = config["Groq:Endpoint"] ?? "https://api.groq.com/openai/v1";
    return new GroqLlmService(apiKey, modelName, endpoint, logger);
});
builder.Services.AddSingleton<ITextToSpeech, MockTtsService>();
builder.Services.AddSingleton<IVoicePipeline, VoicePipelineService>();
builder.Services.AddSingleton<IVoiceCallService, VoiceCallService>();
builder.Services.AddSingleton<ISmsService, MockSmsService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithExposedHeaders("*");
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services, ensureCreated: false);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SafeZone API v1"));
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapHub<Hubs.IncidentHub>("/hubs/incidents");
app.MapHub<Hubs.AlertHub>("/hubs/alerts");
app.MapHub<Hubs.MapHub>("/hubs/map");
app.MapHub<Hubs.CallHub>("/hubs/calls");
app.MapGet("/external-login", async (HttpContext context, string provider, string? returnUrl) =>
{
    var redirectUrl = returnUrl ?? "/";
    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
    await context.ChallengeAsync(provider, properties);
});

app.MapFallbackToPage("/_Host");

app.Run();
```

Key changes:
- `AddIdentityCookies()` replaces manual JWT Bearer config for Blazor pages
- `.AddSignInManager()` on `AddIdentityCore` (required for Blazor auth)
- `AddGoogle()` with config section
- `AddRazorPages()` + `AddServerSideBlazor()` kept
- `MapFallbackToPage("/_Host")` added — all unknown routes go to Blazor
- Removed duplicate/additive `UseDefaultFiles()` since Blazor handles routing

#### Task 1.3: Add Google OAuth config to appsettings

**Modify:** `SafeZone.Server/appsettings.Development.json`

Add before the final `}`:

```json
,"Authentication": {
  "Google": {
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

#### Task 1.4: Create _Host.cshtml

**Create:** `SafeZone.Server/Pages/_Host.cshtml`

```cshtml
@page "/_Host"
@using Microsoft.AspNetCore.Components.Web
@namespace SafeZone.Server.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SafeZone</title>

    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&family=Inter:wght=300;400;500;600;700;800&family=JetBrains+Mono:wght@400;600&display=swap" rel="stylesheet">
    <script src="https://cdn.tailwindcss.com"></script>
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <link rel="stylesheet" href="css/global.css" />

    <base href="~/" />
</head>
<body>
    <component type="typeof(App)" render-mode="ServerPrerendered" />

    <div id="blazor-error-ui" class="hidden fixed bottom-4 right-4 glass-elevated p-4 z-[999]" style="display:none;">
        <p class="text-sm mb-2">An unhandled error has occurred.</p>
        <a href="" class="btn btn-secondary btn-sm">Reload</a>
        <a href="/" class="btn btn-primary btn-sm ml-2">Home</a>
    </div>

    <script src="_framework/blazor.server.js"></script>
    <script src="js/map.js"></script>
</body>
</html>
```

#### Task 1.5: Create App.razor

**Create:** `SafeZone.Server/App.razor`

```razor
@using Microsoft.AspNetCore.Components.Authorization

<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not Found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <div class="min-h-screen flex items-center justify-center">
                    <div class="text-center glass-elevated p-12">
                        <h1 class="font-display text-6xl font-bold mb-4" style="color: var(--danger);">404</h1>
                        <p class="text-muted text-lg mb-6">Page not found</p>
                        <a href="/" class="btn btn-primary">Go Home</a>
                    </div>
                </div>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

#### Task 1.6: Create MainLayout.razor

**Create:** `SafeZone.Server/Components/Layout/MainLayout.razor`

```razor
@inherits LayoutComponentBase
@inject NavigationManager Navigation
@inject AuthenticationStateProvider AuthState
@using Microsoft.AspNetCore.Components.Authorization

<div class="ambient-glow-2"></div>

<nav class="glass fixed top-0 left-0 right-0 z-50" style="border-radius: 0;">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
            <div class="flex items-center gap-3">
                <a href="/" class="flex items-center gap-2">
                    <h1 class="font-display text-xl font-extrabold" style="color: var(--primary);">SafeZone</h1>
                </a>
                @if (Role == "Resident")
                {
                    <span class="badge badge-primary">Resident</span>
                }
                else if (Role == "Authority")
                {
                    <span class="badge badge-purple">Authority</span>
                }
                else if (Role == "SuperAdmin")
                {
                    <span class="badge badge-danger">Admin</span>
                }
            </div>
            <div class="flex items-center gap-4">
                @if (IsAuthenticated)
                {
                    @if (Role == "Resident")
                    {
                        <button id="notifBtn" class="btn btn-secondary btn-icon" title="Notifications" @onclick="() => Navigation.NavigateTo(\"/user/notifications\")">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path>
                            </svg>
                        </button>
                    }
                    <div class="flex items-center gap-3 cursor-pointer">
                        <div class="text-right hidden sm:block">
                            <p class="font-semibold text-sm">@DisplayName</p>
                            <p class="text-muted text-xs">@Role</p>
                        </div>
                        <div class="w-10 h-10 rounded-full flex items-center justify-center font-bold" style="background: var(--primary-dim); color: var(--primary);">
                            @Initials
                        </div>
                    </div>
                    <button class="btn btn-secondary btn-sm" @onclick="Logout">Logout</button>
                }
                else
                {
                    <a href="/login" class="btn btn-secondary btn-sm">Sign In</a>
                    <a href="/register" class="btn btn-primary btn-sm">Get Started</a>
                }
            </div>
        </div>
    </div>
</nav>

<div class="pt-20">
    @Body
</div>

<Toast />
```

#### Task 1.7: Create MainLayout.razor.cs

**Create:** `SafeZone.Server/Components/Layout/MainLayout.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SafeZone.Server.Models;
using System.Security.Claims;

namespace SafeZone.Server.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private SignInManager<User> SignInManager { get; set; } = default!;

    private bool IsAuthenticated;
    private string DisplayName = "";
    private string Role = "";
    private string Initials = "";
    private string AvatarUrl = "";

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        var user = state.User;
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false;

        if (IsAuthenticated)
        {
            DisplayName = user.FindFirst(ClaimTypes.Name)?.Value ?? "User";
            Role = user.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var nameParts = DisplayName.Split(' ');
            Initials = nameParts.Length > 1
                ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
                : DisplayName[..1].ToUpper();
            AvatarUrl = user.FindFirst("avatar")?.Value ?? "";
        }
    }

    private async Task Logout()
    {
        await SignInManager.SignOutAsync();
        Navigation.NavigateTo("/", true);
    }
}
```

#### Task 1.8: Create LoginLayout.razor (minimal layout for auth pages)

**Create:** `SafeZone.Server/Components/Layout/LoginLayout.razor`

```razor
@inherits LayoutComponentBase

<div class="ambient-glow-2"></div>

<nav class="glass fixed top-0 left-0 right-0 z-50" style="border-radius: 0;">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
            <div class="flex items-center gap-3">
                <a href="/" class="flex items-center gap-2 text-sm hover:text-white transition-colors">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path></svg>
                    <span>Back to Home</span>
                </a>
            </div>
            <a href="/">
                <h1 class="font-display text-lg font-bold" style="color: var(--primary);">SafeZone</h1>
            </a>
        </div>
    </div>
</nav>

<div class="pt-20">
    @Body
</div>

<Toast />
```

#### Task 1.9: Create RedirectToLogin component

**Create:** `SafeZone.Server/Components/Shared/RedirectToLogin.razor`

```razor
@using Microsoft.AspNetCore.Components
@inject NavigationManager Navigation

@code {
    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/login", true);
    }
}
```

#### Task 1.10: Create ToastService

**Create:** `SafeZone.Server/Services/ToastService.cs`

```csharp
namespace SafeZone.Server.Services;

public class ToastService
{
    public event Action<string, string>? OnShow;
    public event Action? OnClear;

    public void ShowInfo(string message) => OnShow?.Invoke(message, "info");
    public void ShowSuccess(string message) => OnShow?.Invoke(message, "success");
    public void ShowWarning(string message) => OnShow?.Invoke(message, "warning");
    public void ShowError(string message) => OnShow?.Invoke(message, "error");
    public void Clear() => OnClear?.Invoke();
}
```

Register in Program.cs: add `builder.Services.AddScoped<ToastService>();` before `var app = builder.Build();`

#### Task 1.11: Create Toast.razor

**Create:** `SafeZone.Server/Components/Shared/Toast.razor`

```razor
@using SafeZone.Server.Services
@inject ToastService Toast
@implements IDisposable

<div id="toast-container" class="fixed top-24 right-4 z-[999] flex flex-col gap-2" style="pointer-events: none;">
    @foreach (var toast in Toasts)
    {
        <div class="@($"toast toast-{toast.Type} glass-elevated px-5 py-3 rounded-xl shadow-2xl flex items-center gap-3 min-w-[300px] max-w-[400px]")"
             style="animation: slideIn 0.3s ease-out; pointer-events: auto; border-left: 4px solid @(GetBorderColor(toast.Type));">
            <span>@GetIcon(toast.Type)</span>
            <p class="text-sm flex-1">@toast.Message</p>
            <button class="text-muted hover:text-white text-lg leading-none" @onclick="() => RemoveToast(toast.Id)">&times;</button>
        </div>
    }
</div>

<style>
    .toast {
        transition: all 0.3s ease;
    }
    .toast-info  { border-left-color: var(--info); }
    .toast-success { border-left-color: var(--primary); }
    .toast-warning { border-left-color: var(--warning); }
    .toast-error { border-left-color: var(--danger); }
    @@keyframes slideIn {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
</style>

@code {
    private record ToastItem(Guid Id, string Message, string Type, DateTime CreatedAt);
    private List<ToastItem> Toasts = new();
    private const int MaxToasts = 5;
    private const int DurationMs = 4000;

    protected override void OnInitialized()
    {
        Toast.OnShow += HandleShow;
        Toast.OnClear += HandleClear;
    }

    private void HandleShow(string message, string type)
    {
        var item = new ToastItem(Guid.NewGuid(), message, type, DateTime.UtcNow);
        Toasts.Add(item);
        if (Toasts.Count > MaxToasts) Toasts.RemoveAt(0);
        InvokeAsync(StateHasChanged);
        _ = Task.Delay(DurationMs).ContinueWith(_ =>
        {
            RemoveToast(item.Id);
            InvokeAsync(StateHasChanged);
        });
    }

    private void HandleClear()
    {
        Toasts.Clear();
        InvokeAsync(StateHasChanged);
    }

    private void RemoveToast(Guid id)
    {
        Toasts.RemoveAll(t => t.Id == id);
        InvokeAsync(StateHasChanged);
    }

    private string GetIcon(string type) => type switch
    {
        "success" => "✅",
        "warning" => "⚠️",
        "error" => "❌",
        _ => "ℹ️"
    };

    private string GetBorderColor(string type) => type switch
    {
        "success" => "var(--primary)",
        "warning" => "var(--warning)",
        "error" => "var(--danger)",
        _ => "var(--info)"
    };

    public void Dispose()
    {
        Toast.OnShow -= HandleShow;
        Toast.OnClear -= HandleClear;
    }
}
```

#### Task 1.12: Create shared utility components

**Create:** `SafeZone.Server/Components/Shared/LoadingSpinner.razor`

```razor
<div class="flex items-center justify-center py-12">
    <div class="spinner"></div>
</div>
```

**Create:** `SafeZone.Server/Components/Shared/StatCard.razor`

```razor
<div class="stats-card p-4 rounded-xl" style="background: var(--glass); border: 1px solid var(--glass-border);">
    <p class="text-xs text-muted mb-1">@Label</p>
    <p class="font-mono font-bold text-3xl" style="color: @Color;">@Value</p>
</div>

@code {
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string Color { get; set; } = "var(--text-1)";
}
```

**Create:** `SafeZone.Server/Components/Shared/Badge.razor`

```razor
<span class="@($"badge badge-{Variant}")">@ChildContent</span>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Variant { get; set; } = "primary";
}
```

**Create:** `SafeZone.Server/Components/Shared/ConfirmDialog.razor`

```razor
@if (IsVisible)
{
    <div class="fixed inset-0 z-[100] flex items-center justify-center" style="background: rgba(0,0,0,0.6);">
        <div class="glass-elevated p-6 rounded-xl max-w-md w-full mx-4" style="animation: fadeIn 0.2s ease;">
            <h3 class="font-display text-lg font-bold mb-2">@Title</h3>
            <p class="text-muted mb-6">@Message</p>
            <div class="flex justify-end gap-3">
                <button class="btn btn-secondary" @onclick="Cancel">Cancel</button>
                <button class="btn btn-danger" @onclick="Confirm">@ConfirmText</button>
            </div>
        </div>
    </div>
}

@code {
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string Title { get; set; } = "Confirm";
    [Parameter] public string Message { get; set; } = "Are you sure?";
    [Parameter] public string ConfirmText { get; set; } = "Confirm";
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task Confirm() => await OnConfirm.InvokeAsync();
    private async Task Cancel() => await OnCancel.InvokeAsync();
}
```

#### Task 1.13: Clean up wwwroot

Remove files that are replaced by Blazor:
- All 17 `*.html` files in `wwwroot/`, `wwwroot/user/`, `wwwroot/authority/`
- `wwwroot/js/auth.js`, `wwwroot/js/api.js`, `wwwroot/js/toast.js`, `wwwroot/js/signalr-client.js`, `wwwroot/js/geolocation.js`
- Keep: `wwwroot/css/global.css`, `wwwroot/js/map.js`, any images/assets

### Phase 2: Auth Pages

#### Task 2.1: Create Login.razor

**Create:** `SafeZone.Server/Components/Pages/Login.razor`

Page layout matches the existing `login.html` visually. Added features:
- Email/password form with validation
- "Sign in with Google" button that links to `/signin-google`
- Error message display
- On success, redirect to `/user/dashboard` or `/authority/dashboard` based on role

(Full code generated at execution, but follows the HTML structure of login.html, replacing JS logic with Blazor form submission using SignInManager)

#### Task 2.2: Create Register.razor

**Create:** `SafeZone.Server/Components/Pages/Register.razor`

Matches the existing `register.html` visually. Email, password, confirm password, role selection fields. Creates user via UserManager, signs in, redirects based on role.

(Full code generated at execution)

#### Task 2.3: Remove old auth pages

Delete `login.html`, `register.html`

### Phase 3: Resident Pages

#### Task 3.1-3.8: Create each resident page

All 8 resident pages:

| Page | Route | Replaces |
|------|-------|----------|
| Dashboard | `/user/dashboard` | `user/dashboard.html` |
| ReportIncident | `/user/report-incident` | `user/report-incident.html` |
| Sos | `/user/sos` | `user/sos.html` |
| FileFir | `/user/file-fir` | `user/file-fir.html` |
| MyFirs | `/user/my-firs` | `user/my-firs.html` |
| MyIncidents | `/user/my-incidents` | `user/my-incidents.html` |
| Notifications | `/user/notifications` | `user/notifications.html` |
| WeatherMap | `/user/weather-map` | `user/weather-map.html` |

Each page:
- Uses `@page "/user/..."` and `@attribute [Authorize(Roles = "Resident")]`
- Injects relevant services instead of calling API
- Replaces JS fetch calls with direct C# method calls
- Replaces JS template rendering with Blazor loops (`@foreach`)
- Replaces JS event handlers with Blazor event handlers (`@onclick`, `@onchange`, etc.)
- Keeps HTML structure identical for visual consistency
- Moves inline `<style>` blocks into the `.razor` file's `<style>` section
- Leaflet maps use JS interop (calls to `window.mapFunctions.*`)

(Framework documented here; full code for each page generated at execution)

### Phase 4: Authority Pages

#### Task 4.1-4.7: Create each authority page

| Page | Route | Replaces |
|------|-------|----------|
| Dashboard | `/authority/dashboard` | `authority/dashboard.html` |
| Incidents | `/authority/incidents` | `authority/incidents.html` |
| Kanban | `/authority/kanban` | `authority/kanban.html` |
| Firs | `/authority/firs` | `authority/firs.html` |
| SosLogs | `/authority/sos-logs` | `authority/sos-logs.html` |
| BroadcastAlert | `/authority/broadcast-alert` | `authority/broadcast-alert.html` |
| AiAgent | `/authority/ai-agent` | `authority/ai-agent.html` |

Same approach as Phase 3. Kanban drag-and-drop to be handled via JS interop.

### Phase 5: Index Page

#### Task 5.1: Create Index.razor

**Create:** `SafeZone.Server/Components/Pages/Index.razor`

- Route: `@page "/"`
- No auth required (available to all)
- Hero section, feature cards, tech stack section, CTA section
- Three.js 3D sphere via JS interop
- Counter animation via C# timer
- Matches `index.html` visually

#### Task 5.2: Remove index.html

---

### Build & Verify

#### Task 6.1: Verify compilation

Run: `dotnet build` in `SafeZone.Server/` directory

#### Task 6.2: Smoke test

Run: `dotnet run` and verify that:
- `/` loads the landing page
- `/login` loads the login form
- Google OAuth button is visible
- Register works
- Role-based redirects work
- Resident pages load after login
- Authority pages load after login
