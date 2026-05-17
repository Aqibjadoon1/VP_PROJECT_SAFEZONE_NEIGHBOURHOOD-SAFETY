# Blazor Server Conversion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal**: Convert SafeZone.Server from Web API to Blazor Server without changing any existing functionality, UI, or files.

**Architecture**: Minimal changes only. Add Blazor service registrations and hub mapping. Keep all existing files and behavior unchanged.

**Tech Stack**: .NET 8, Blazor Server, ASP.NET Core Web API (co-existing)

---

## File Structure

| File | Action | What changes |
|------|--------|--------------|
| `SafeZone.Server.csproj` | Modify | Add `<RazorLangVersion>` |
| `Program.cs` | Modify | Add 3 lines: `AddRazorPages()`, `AddServerSideBlazor()`, `MapBlazorHub()` |

**No other files are touched.**

---

## Task 1: Update csproj

**Files:**
- Modify: `SafeZone.Server/SafeZone.Server.csproj:3-7`

- [ ] **Step 1: Add RazorLangVersion to PropertyGroup**

Add inside the `<PropertyGroup>` (after `<Nullable>enable</Nullable>`):

```xml
<RazorLangVersion>Latest</RazorLangVersion>
```

Final csproj PropertyGroup:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <RazorLangVersion>Latest</RazorLangVersion>
</PropertyGroup>
```

- [ ] **Step 2: Verify no other changes needed**

Blazor Server NuGet packages (`Microsoft.AspNetCore.Components.Server`, etc.) are already included implicitly via `Microsoft.NET.Sdk.Web`. No additional package references needed.

- [ ] **Step 3: Build to confirm csproj change compiles**

Run: `dotnet build`
Expected: Build succeeded with 0 errors.

---

## Task 2: Update Program.cs - Service Registrations

**Files:**
- Modify: `SafeZone.Server/Program.cs:110-117` (after `AddSignalR()`, before service scoped registrations)

- [ ] **Step 1: Find the right location**

Right after this line (line 110):
```csharp
builder.Services.AddSignalR();
```

Add these two lines:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
```

So that section becomes:
```csharp
builder.Services.AddSignalR();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddScoped<SafeZone.Server.Services.IAuthService, SafeZone.Server.Services.AuthService>();
// ... rest stays the same
```

- [ ] **Step 2: Build to confirm service registrations compile**

Run: `dotnet build`
Expected: Build succeeded.

---

## Task 3: Update Program.cs - Middleware/Endpoint Mapping

**Files:**
- Modify: `SafeZone.Server/Program.cs:158-162` (between `MapControllers()` and existing hub mappings)

- [ ] **Step 1: Add MapBlazorHub() after MapControllers()**

Find this section (lines 158-162):
```csharp
app.MapControllers();

app.MapHub<SafeZone.Server.Hubs.IncidentHub>("/hubs/incidents");
app.MapHub<SafeZone.Server.Hubs.AlertHub>("/hubs/alerts");
app.MapHub<SafeZone.Server.Hubs.MapHub>("/hubs/map");
```

Add `app.MapBlazorHub();` AFTER `MapControllers()` but BEFORE the existing hub mappings:

```csharp
app.MapControllers();

app.MapBlazorHub();

app.MapHub<SafeZone.Server.Hubs.IncidentHub>("/hubs/incidents");
app.MapHub<SafeZone.Server.Hubs.AlertHub>("/hubs/alerts");
app.MapHub<SafeZone.Server.Hubs.MapHub>("/hubs/map");
```

**Important**: Do NOT add `MapFallbackToPage("/_Host")`. This would intercept static file routing. Without this fallback:
- All existing static HTML files (`wwwroot/**/*.html`) continue to work normally
- Blazor Hub is available at `/_blazor` for future component use
- No Blazor page routing is enabled unless explicitly added

- [ ] **Step 2: Full build verification**

Run: `dotnet build`
Expected: Build succeeded with 0 errors.

---

## Task 4: Integration Test - Run Server and Verify

**Files:**
- None - just runtime verification

- [ ] **Step 1: Kill any existing dotnet processes on port 5000/5002**

Run: `netstat -ano | findstr "5000"` to find PIDs, then `taskkill /F /PID <pid>`

- [ ] **Step 2: Run the server**

Run: `dotnet run`
Expected output includes:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5002  (or port from launchSettings)
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

- [ ] **Step 3: Verify existing URLs work**

Verify these endpoints respond correctly:

1. **Landing page (static HTML)**
   - URL: `http://localhost:5002/` (or whatever port it's on)
   - Expected: Returns `wwwroot/index.html` content

2. **Swagger UI (API)**
   - URL: `http://localhost:5002/swagger`
   - Expected: Swagger UI loads

3. **API endpoint test**
   - URL: `GET http://localhost:5002/api/incident/categories`
   - Expected: JSON array of categories

- [ ] **Step 4: Verify Blazor Hub is available**

The Blazor hub endpoint `/_blazor` should now exist. This confirms the conversion worked.

---

## Summary of Changes Made

| File | Change Type | Exact Change |
|------|-------------|--------------|
| `SafeZone.Server.csproj` | Add | `<RazorLangVersion>Latest</RazorLangVersion>` |
| `Program.cs` | Add | `builder.Services.AddRazorPages();` |
| `Program.cs` | Add | `builder.Services.AddServerSideBlazor();` |
| `Program.cs` | Add | `app.MapBlazorHub();` |

**4 lines total. 0 files removed. 0 behaviors changed.**

---

## Verification Checklist

- [ ] Build succeeds: `dotnet build` → 0 errors
- [ ] Server runs: `dotnet run` → No exceptions on startup
- [ ] Static files work: `GET /` → Returns `wwwroot/index.html`
- [ ] APIs work: `GET /api/incident/categories` → Returns JSON
- [ ] Swagger works: `/swagger` → UI loads
- [ ] Project is now technically "Blazor Server" while keeping 100% existing behavior
