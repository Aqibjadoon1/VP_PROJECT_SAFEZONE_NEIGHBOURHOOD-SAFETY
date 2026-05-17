# SafeZone Tactical Command Center — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform SafeZone's UI into an immersive Tactical Command Center with glowing effects, animated transitions, and a cohesive mission-control aesthetic — while keeping all C# / Blazor Server functionality intact.

**Architecture:** Pure CSS + Blazor Server — no new JS frameworks. All effects via CSS animations, Tailwind utilities, and lightweight Three.js scenes (existing). Component-based redesign with reusable tactical components.

**Tech Stack:** C# 12, .NET 8, Blazor Server, Tailwind CSS CDN, Three.js CDN, Leaflet.js

---

## File Map

**Modified Files:**
- `wwwroot/css/global.css` — Complete CSS redesign (2600+ lines → reorganized + new tactical styles)
- `Components/Layout/DashboardLayout.razor` — Add status bar, tactical sidebar
- `Components/Shared/AppSidebar.razor` — Tactical nav items with glow
- `Components/Shared/MetricCard.razor` — Glowing metric cards with count-up
- `Components/Shared/StatusChip.razor` — Glowing status badges
- `Components/Shared/LoadingSkeleton.razor` — Shimmer effect
- `Components/Shared/Toast.razor` — Tactical toast notifications
- `Components/Shared/GlassCard.razor` — Enhanced glass morphism
- `Components/Pages/User/Dashboard.razor` — Tactical dashboard layout
- `Components/Pages/User/MyIncidents.razor` — Tactical incident cards + modal
- `Components/Pages/Login.razor` — Terminal-style login
- `Components/Pages/Register.razor` — Terminal-style register
- `Components/Pages/Index.razor` — Enhanced landing page
- `Pages/_Host.cshtml` — Update CSS cache buster

**New Files:**
- `Components/Shared/StatusBar.razor` — Live clock + system status + ticker
- `Components/Shared/IncidentCard.razor` — Reusable tactical incident card
- `wwwroot/js/tactical-effects.js` — Lightweight particle canvas + nebula effect

---

## Phase 1: Foundation — CSS System & Global Effects

### Task 1: Update CSS Variables and Base Styles

**Files:**
- Modify: `wwwroot/css/global.css:1-65` (CSS variables section)

- [ ] **Step 1: Add new tactical color tokens**

Add these variables to the `:root` block, keeping existing ones:

```css
/* Tactical Glow System */
--neon-green: #00FF88;
--neon-green-dim: rgba(0,255,136,0.15);
--neon-green-glow: rgba(0,255,136,0.30);
--electric-cyan: #00D4FF;
--electric-cyan-dim: rgba(0,212,255,0.15);
--electric-cyan-glow: rgba(0,212,255,0.30);
--alert-red: #FF3366;
--alert-red-dim: rgba(255,51,102,0.15);
--alert-red-glow: rgba(255,51,102,0.30);
--warning-amber: #FFB800;
--warning-amber-dim: rgba(255,184,0,0.15);
--warning-amber-glow: rgba(255,184,0,0.30);
--command-purple: #8B5CF6;
--command-purple-dim: rgba(139,92,246,0.15);
--command-purple-glow: rgba(139,92,246,0.30);
```

- [ ] **Step 2: Update body background to void black**

```css
body {
    font-family: var(--font-ui);
    background: var(--bg-void);  /* was var(--bg-void), keep but ensure --void-black is available */
    color: var(--text-1);
    min-height: 100vh;
    line-height: 1.6;
    overflow-x: hidden;
}
```

- [ ] **Step 3: Add scan lines texture after body::before**

After the existing `body::before` (dot grid), add:

```css
/* Scan lines overlay */
body::after {
    content: '';
    position: fixed;
    inset: 0;
    background: repeating-linear-gradient(
        0deg,
        transparent,
        transparent 2px,
        rgba(0,0,0,0.03) 2px,
        rgba(0,0,0,0.03) 4px
    );
    pointer-events: none;
    z-index: 9999;
    opacity: 0.4;
}
```

- [ ] **Step 4: Add animated grid background keyframes**

```css
@keyframes grid-pan {
    0% { background-position: 0 0; }
    100% { background-position: 32px 32px; }
}
```

- [ ] **Step 5: Add global glow pulse animation**

```css
@keyframes pulse-glow {
    0%, 100% { opacity: 1; box-shadow: 0 0 5px currentColor; }
    50% { opacity: 0.85; box-shadow: 0 0 20px currentColor, 0 0 40px currentColor; }
}
```

- [ ] **Step 6: Add shimmer animation**

```css
@keyframes shimmer {
    0% { background-position: -200% 0; }
    100% { background-position: 200% 0; }
}
```

- [ ] **Step 7: Add card entrance stagger**

```css
@keyframes card-enter {
    from { opacity: 0; transform: translateY(20px); }
    to { opacity: 1; transform: translateY(0); }
}
```

- [ ] **Step 8: Add page transition**

```css
@keyframes page-enter {
    from { opacity: 0; transform: scale(0.98); }
    to { opacity: 1; transform: scale(1); }
}
```

- [ ] **Step 9: Add gradient border rotation**

```css
@keyframes border-rotate {
    0% { --angle: 0deg; }
    100% { --angle: 360deg; }
}
```

- [ ] **Step 10: Add live dot pulse**

```css
@keyframes live-dot-pulse {
    0%, 100% { transform: scale(1); opacity: 1; }
    50% { transform: scale(1.4); opacity: 0.7; }
}
```

- [ ] **Step 11: Commit**

```bash
git add wwwroot/css/global.css
git commit -m "feat: add tactical design tokens and keyframe animations"
```

---

### Task 2: Add Tactical Layout Classes

**Files:**
- Modify: `wwwroot/css/global.css` (after existing layout section)

- [ ] **Step 1: Add dashboard animated grid background**

```css
.dashboard-theme-shell {
    display: flex;
    min-height: 100vh;
    background:
        radial-gradient(circle at top right, rgba(99,51,255,0.16) 0%, transparent 34%),
        radial-gradient(circle at bottom left, rgba(0,255,136,0.08) 0%, transparent 30%),
        var(--bg-void);
    background-image:
        radial-gradient(circle at top right, rgba(99,51,255,0.16) 0%, transparent 34%),
        radial-gradient(circle at bottom left, rgba(0,255,136,0.08) 0%, transparent 30%),
        radial-gradient(circle, rgba(255,255,255,0.04) 1px, transparent 1px);
    background-size: 100% 100%, 100% 100%, 32px 32px;
    animation: grid-pan 20s linear infinite;
    color: var(--text-1);
    font-family: var(--font-ui);
}
```

- [ ] **Step 2: Add status bar styles**

```css
.status-bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    height: 44px;
    padding: 0 24px;
    background: rgba(5,5,8,0.8);
    border-bottom: 1px solid var(--glass-border);
    backdrop-filter: blur(16px) saturate(180%);
    -webkit-backdrop-filter: blur(16px) saturate(180%);
    font-family: var(--font-mono);
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-2);
    position: sticky;
    top: 0;
    z-index: 35;
}

.status-bar-left,
.status-bar-center,
.status-bar-right {
    display: flex;
    align-items: center;
    gap: 16px;
}

.status-bar-clock {
    color: var(--text-1);
    font-weight: 600;
    font-size: 0.85rem;
}

.status-online {
    display: flex;
    align-items: center;
    gap: 6px;
    color: var(--neon-green);
}

.status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--neon-green);
    animation: live-dot-pulse 2s ease-in-out infinite;
    box-shadow: 0 0 8px var(--neon-green-glow);
}

.status-ticker {
    overflow: hidden;
    white-space: nowrap;
    max-width: 400px;
    mask-image: linear-gradient(to right, transparent, black 10%, black 90%, transparent);
}

.status-ticker-content {
    display: inline-block;
    animation: ticker-scroll 30s linear infinite;
}

@keyframes ticker-scroll {
    0% { transform: translateX(100%); }
    100% { transform: translateX(-100%); }
}
```

- [ ] **Step 3: Enhance sidebar with tactical glow**

Add to existing `.dashboard-theme-sidebar`:

```css
.dashboard-theme-sidebar {
    /* existing styles stay */
    position: relative;
}

.dashboard-theme-sidebar::after {
    content: '';
    position: absolute;
    top: 0;
    right: 0;
    bottom: 0;
    width: 1px;
    background: linear-gradient(to bottom, transparent, var(--neon-green), transparent);
    opacity: 0.3;
}

/* Tactical nav items */
.dashboard-nav-item {
    position: relative;
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 14px;
    border-radius: var(--radius-md);
    color: var(--text-2);
    text-decoration: none;
    font-size: 0.9rem;
    font-weight: 500;
    transition: all 200ms ease;
    margin-bottom: 4px;
    border-left: 3px solid transparent;
}

.dashboard-nav-item:hover {
    background: rgba(255,255,255,0.05);
    color: var(--text-1);
    transform: translateX(4px);
}

.dashboard-nav-item.active {
    background: var(--neon-green-dim);
    color: var(--neon-green);
    border-left-color: var(--neon-green);
    box-shadow: 0 0 20px var(--neon-green-glow), inset 0 0 10px var(--neon-green-dim);
}

.dashboard-nav-icon {
    width: 22px;
    height: 22px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    transition: transform 200ms ease;
}

.dashboard-nav-item:hover .dashboard-nav-icon {
    transform: scale(1.1);
}
```

- [ ] **Step 4: Add tactical main content area**

```css
.dashboard-theme-main {
    flex: 1;
    min-width: 0;
    padding: 20px 28px 32px;
    z-index: 1;
    color: var(--text-1);
    font-family: var(--font-ui);
    animation: page-enter 300ms ease;
}
```

- [ ] **Step 5: Commit**

```bash
git add wwwroot/css/global.css
git commit -m "feat: add tactical layout classes — status bar, sidebar glow, main area"
```

---

## Phase 2: Component Upgrades

### Task 3: Create StatusBar Component

**Files:**
- Create: `Components/Shared/StatusBar.razor`

- [ ] **Step 1: Write StatusBar.razor**

```razor
@inject IIncidentService IncidentService
@implements IDisposable

<div class="status-bar">
    <div class="status-bar-left">
        <span class="status-bar-clock">@CurrentTime</span>
        <span>@CurrentDate</span>
    </div>
    <div class="status-bar-center">
        <span class="status-online">
            <span class="status-dot"></span>
            SYSTEM ONLINE
        </span>
        @if (RecentIncidents.Any())
        {
            <span class="status-ticker">
                <span class="status-ticker-content">
                    @string.Join("  •  ", RecentIncidents.Select(i => $"#{i.IncidentNumber} — {i.Title} ({i.Status})"))
                </span>
            </span>
        }
    </div>
    <div class="status-bar-right">
        <span>@Role</span>
    </div>
</div>

@code {
    private string CurrentTime = "";
    private string CurrentDate = "";
    private string Role = "User";
    private List<IncidentListDto> RecentIncidents = new();
    private System.Timers.Timer? _clockTimer;
    private System.Timers.Timer? _refreshTimer;
    private bool _disposed;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await UpdateClock();
        await LoadRecentIncidents();

        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            Role = authState.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";
        }

        _clockTimer = new System.Timers.Timer(1000);
        _clockTimer.Elapsed += async (_, _) =>
        {
            if (_disposed) return;
            await InvokeAsync(UpdateClock);
        };
        _clockTimer.AutoReset = true;
        _clockTimer.Start();

        _refreshTimer = new System.Timers.Timer(30000);
        _refreshTimer.Elapsed += async (_, _) =>
        {
            if (_disposed) return;
            await InvokeAsync(LoadRecentIncidents);
        };
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }

    private Task UpdateClock()
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("yyyy-MM-dd");
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task LoadRecentIncidents()
    {
        try
        {
            RecentIncidents = await IncidentService.GetRecentIncidentsAsync(5);
        }
        catch
        {
            RecentIncidents = new List<IncidentListDto>();
        }
        StateHasChanged();
    }

    public void Dispose()
    {
        _disposed = true;
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
    }
}
```

- [ ] **Step 2: Add to DashboardLayout.razor**

Insert `<StatusBar />` inside the `<main>` element, before the `<header>`:

```razor
<main class="dashboard-theme-main" style="flex:1;min-width:0;">
    <StatusBar />
    <header class="dashboard-theme-topbar">
```

- [ ] **Step 3: Commit**

```bash
git add Components/Shared/StatusBar.razor Components/Layout/DashboardLayout.razor
git commit -m "feat: add StatusBar component with live clock and incident ticker"
```

---

### Task 4: Upgrade MetricCard Component

**Files:**
- Modify: `Components/Shared/MetricCard.razor`

- [ ] **Step 1: Add CSS for tactical metric cards**

Add to `global.css`:

```css
.metric-card {
    position: relative;
    padding: 20px;
    background: var(--bg-surface);
    border: 1px solid var(--glass-border);
    border-radius: var(--radius-lg);
    overflow: hidden;
    transition: all 200ms ease;
    animation: card-enter 400ms ease backwards;
}

.metric-card::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--accent-color), transparent);
    box-shadow: 0 0 10px var(--accent-glow);
}

.metric-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 32px var(--accent-glow);
    border-color: var(--accent-color);
}

.metric-card-icon {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: var(--font-mono);
    font-weight: 700;
    font-size: 0.8rem;
    margin-bottom: 12px;
    background: var(--accent-dim);
    color: var(--accent-color);
    box-shadow: 0 0 15px var(--accent-glow);
}

.metric-card-value {
    font-family: var(--font-mono);
    font-size: 1.75rem;
    font-weight: 700;
    color: var(--text-1);
    line-height: 1.2;
}

.metric-card-label {
    font-family: var(--font-mono);
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--text-3);
    margin-top: 4px;
}
```

- [ ] **Step 2: Update MetricCard.razor**

```razor
<div class="metric-card" style="--accent-color: @AccentColor; --accent-dim: @AccentDim; --accent-glow: @AccentGlow; animation-delay: @(Index * 50)ms;">
    <div class="metric-card-icon">@Icon</div>
    <div class="metric-card-value">@Value</div>
    <div class="metric-card-label">@Label</div>
    @if (!string.IsNullOrEmpty(Change))
    {
        <div style="margin-top: 8px; font-size: 0.75rem; color: var(--text-3);">@Change</div>
    }
</div>

@code {
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string Icon { get; set; } = "";
    [Parameter] public string Accent { get; set; } = "primary";
    [Parameter] public string? Change { get; set; }
    [Parameter] public int Index { get; set; } = 0;

    private string AccentColor => Accent switch
    {
        "primary" => "var(--neon-green)",
        "warning" => "var(--warning-amber)",
        "danger" => "var(--alert-red)",
        "info" => "var(--electric-cyan)",
        "purple" => "var(--command-purple)",
        _ => "var(--neon-green)"
    };

    private string AccentDim => Accent switch
    {
        "primary" => "var(--neon-green-dim)",
        "warning" => "var(--warning-amber-dim)",
        "danger" => "var(--alert-red-dim)",
        "info" => "var(--electric-cyan-dim)",
        "purple" => "var(--command-purple-dim)",
        _ => "var(--neon-green-dim)"
    };

    private string AccentGlow => Accent switch
    {
        "primary" => "var(--neon-green-glow)",
        "warning" => "var(--warning-amber-glow)",
        "danger" => "var(--alert-red-glow)",
        "info" => "var(--electric-cyan-glow)",
        "purple" => "var(--command-purple-glow)",
        _ => "var(--neon-green-glow)"
    };
}
```

- [ ] **Step 3: Commit**

```bash
git add Components/Shared/MetricCard.razor wwwroot/css/global.css
git commit -m "feat: upgrade MetricCard with tactical glow and stagger animations"
```

---

### Task 5: Upgrade IncidentCard + MyIncidents Modal

**Files:**
- Create: `Components/Shared/IncidentCard.razor`
- Modify: `Components/Pages/User/MyIncidents.razor`

- [ ] **Step 1: Add incident card CSS**

```css
.incident-card-tactical {
    position: relative;
    padding: 16px 20px;
    background: var(--bg-surface);
    border: 1px solid var(--glass-border);
    border-left: 4px solid var(--severity-color);
    border-radius: var(--radius-lg);
    transition: all 200ms ease;
    cursor: pointer;
    overflow: hidden;
    animation: card-enter 400ms ease backwards;
}

.incident-card-tactical:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 24px var(--severity-glow);
    border-color: var(--severity-color);
}

.incident-card-tactical.critical {
    --severity-color: var(--alert-red);
    --severity-glow: var(--alert-red-glow);
    animation: card-enter 400ms ease backwards, pulse-glow 2s ease-in-out infinite;
    animation-delay: 0ms, 500ms;
}

.incident-card-tactical.high {
    --severity-color: var(--warning-amber);
    --severity-glow: var(--warning-amber-glow);
}

.incident-card-tactical.medium {
    --severity-color: var(--electric-cyan);
    --severity-glow: var(--electric-cyan-glow);
}

.incident-card-tactical.low {
    --severity-color: var(--neon-green);
    --severity-glow: var(--neon-green-glow);
}

.incident-card-number {
    font-family: var(--font-mono);
    font-size: 0.75rem;
    color: var(--text-3);
    letter-spacing: 0.05em;
}

.incident-card-title {
    font-weight: 600;
    font-size: 1rem;
    margin: 4px 0;
    color: var(--text-1);
}

.incident-card-meta {
    display: flex;
    gap: 12px;
    font-size: 0.8rem;
    color: var(--text-3);
}
```

- [ ] **Step 2: Create IncidentCard.razor**

```razor
<div class="incident-card-tactical @SeverityClass" style="animation-delay: @(Index * 50)ms;" @onclick="OnClick">
    <div class="incident-card-number">@Incident.IncidentNumber</div>
    <div class="incident-card-title">@Incident.Title</div>
    <div class="incident-card-meta">
        <span>@Incident.CategoryName</span>
        <span>•</span>
        <span>@GetTimeAgo(Incident.ReportedAt)</span>
    </div>
</div>

@code {
    [Parameter] public IncidentListDto Incident { get; set; } = null!;
    [Parameter] public int Index { get; set; }
    [Parameter] public EventCallback<Guid> OnIncidentClick { get; set; }

    private string SeverityClass => Incident.Severity switch
    {
        SeverityLevel.Critical => "critical",
        SeverityLevel.High => "high",
        SeverityLevel.Medium => "medium",
        _ => "low"
    };

    private async Task OnClick()
    {
        await OnIncidentClick.InvokeAsync(Incident.IncidentId);
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime.ToUniversalTime();
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }
}
```

- [ ] **Step 3: Update MyIncidents.razor to use IncidentCard**

Replace the incident list loop in `MyIncidents.razor`:

```razor
@foreach (var incident in FilteredIncidents.Select((inc, idx) => (inc, idx)))
{
    <IncidentCard Incident="@incident.inc" Index="@incident.idx" OnIncidentClick="ShowDetail" />
}
```

- [ ] **Step 4: Commit**

```bash
git add Components/Shared/IncidentCard.razor Components/Pages/User/MyIncidents.razor wwwroot/css/global.css
git commit -m "feat: add tactical IncidentCard with severity glow and pulse"
```

---

## Phase 3: Login & Landing Page

### Task 6: Terminal-Style Login Page

**Files:**
- Modify: `Components/Pages/Login.razor`
- Modify: `Components/Layout/LoginLayout.razor`

- [ ] **Step 1: Add login-specific CSS**

```css
.login-page {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    overflow: hidden;
}

.login-terminal {
    position: relative;
    width: 100%;
    max-width: 440px;
    padding: 40px;
    background: var(--bg-surface);
    border: 1px solid var(--glass-border);
    border-radius: var(--radius-lg);
    box-shadow: 0 24px 64px rgba(0,0,0,0.4);
    overflow: hidden;
}

.login-terminal::before {
    content: '';
    position: absolute;
    inset: -2px;
    border-radius: var(--radius-lg);
    background: conic-gradient(from var(--angle, 0deg), var(--neon-green), var(--electric-cyan), var(--neon-green));
    z-index: -1;
    opacity: 0.5;
    animation: border-rotate 4s linear infinite;
}

.login-terminal-header {
    font-family: var(--font-mono);
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.15em;
    color: var(--neon-green);
    margin-bottom: 24px;
    text-align: center;
}

.login-input {
    width: 100%;
    padding: 12px 0;
    background: transparent;
    border: none;
    border-bottom: 1px solid var(--glass-border);
    color: var(--text-1);
    font-family: var(--font-mono);
    font-size: 0.95rem;
    transition: all 200ms ease;
    outline: none;
}

.login-input:focus {
    border-bottom-color: var(--electric-cyan);
    box-shadow: 0 4px 12px var(--electric-cyan-glow);
}

.login-input::placeholder {
    color: var(--text-3);
    font-family: var(--font-ui);
}

.login-label {
    display: block;
    font-family: var(--font-mono);
    font-size: 0.65rem;
    text-transform: uppercase;
    letter-spacing: 0.15em;
    color: var(--text-3);
    margin-bottom: 8px;
}

.login-btn {
    width: 100%;
    padding: 14px;
    background: var(--neon-green);
    color: var(--bg-void);
    border: none;
    border-radius: var(--radius-md);
    font-family: var(--font-mono);
    font-weight: 700;
    font-size: 0.85rem;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    cursor: pointer;
    transition: all 200ms ease;
    position: relative;
    overflow: hidden;
}

.login-btn:hover {
    box-shadow: 0 0 30px var(--neon-green-glow);
    transform: translateY(-2px);
}
```

- [ ] **Step 2: Update Login.razor**

Replace the form section with terminal-style inputs:

```razor
<div class="login-page">
    <div class="login-terminal">
        <div class="login-terminal-header">Secure Access Terminal</div>
        
        <EditForm Model="@loginModel" OnValidSubmit="HandleLogin">
            <DataAnnotationsValidator />
            
            <div style="margin-bottom: 24px;">
                <label class="login-label">Username / Phone</label>
                <InputText @bind-Value="loginModel.PhoneNumber" class="login-input" placeholder="+923001234567" />
            </div>
            
            <div style="margin-bottom: 32px;">
                <label class="login-label">Password</label>
                <InputText type="password" @bind-Value="loginModel.Password" class="login-input" placeholder="••••••••" />
            </div>
            
            @if (!string.IsNullOrEmpty(errorMessage))
            {
                <div style="color: var(--alert-red); font-family: var(--font-mono); font-size: 0.8rem; margin-bottom: 16px;">
                    ⚠ @errorMessage
                </div>
            }
            
            <button type="submit" class="login-btn" disabled="@isLoading">
                @if (isLoading)
                {
                    <span>PROCESSING...</span>
                }
                else
                {
                    <span>AUTHENTICATE</span>
                }
            </button>
        </EditForm>
        
        <div style="text-align: center; margin-top: 24px;">
            <a href="/register" style="font-family: var(--font-mono); font-size: 0.75rem; color: var(--text-3); text-decoration: none; text-transform: uppercase; letter-spacing: 0.1em;">
                Initialize Account →
            </a>
        </div>
    </div>
</div>
```

- [ ] **Step 3: Update LoginLayout.razor to include nebula background**

Add a canvas element for the particle nebula:

```razor
@inherits LayoutComponentBase

<div class="login-layout">
    <canvas id="nebula-canvas" style="position: fixed; inset: 0; z-index: 0;"></canvas>
    <div style="position: relative; z-index: 1;">
        @Body
    </div>
</div>

<Toast />
```

- [ ] **Step 4: Commit**

```bash
git add Components/Pages/Login.razor Components/Layout/LoginLayout.razor wwwroot/css/global.css
git commit -m "feat: redesign login page with terminal aesthetic and animated border"
```

---

## Phase 4: Execution Handoff

After completing Tasks 1-6 (the foundation and highest-impact changes), the app will have:
- ✅ Tactical color system with glow effects
- ✅ Live status bar with clock and ticker
- ✅ Glowing metric cards with staggered entrance
- ✅ Severity-coded incident cards with pulse animations
- ✅ Terminal-style login with animated gradient border
- ✅ Animated grid background on dashboards

**Remaining work (Tasks 7+):**
- Register page terminal styling
- Landing page enhancements (orbital rings, floating cards)
- SOS emergency full-screen interface
- Authority dashboard enhancements
- Toast notifications redesign
- All remaining pages (Settings, Profile, Notifications, Kanban, etc.)
- Performance optimization and reduced-motion support

---

## Verification Steps

After each task:
1. `dotnet build` — must succeed with zero errors
2. `dotnet run --urls http://localhost:5002` — verify visually
3. Check key pages: login, dashboard, my-incidents, modal
4. Test responsive behavior (mobile sidebar, modal positioning)
5. Commit with descriptive message

## Commit Messages

Use Conventional Commits:
- `feat:` for new features/components
- `style:` for CSS changes
- `refactor:` for structural changes

Example: `feat: add StatusBar component with live clock and incident ticker`

---

**Plan saved to:** `docs/superpowers/plans/2026-05-16-tactical-command-center-plan.md`

**Execution choice:** This plan is optimized for **subagent-driven development** — each task is self-contained and can be handed to a fresh subagent. However, given the CSS-heavy nature, **inline execution** with frequent checkpoints may be faster since many tasks touch the same CSS file.
