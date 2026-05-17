# SafeZone - Blazor Server Conversion Spec

**Date**: 2026-05-10

**Goal**: Convert SafeZone.Server from ASP.NET Core Web API to Blazor Server, WITHOUT changing any existing functionality, UI, or files.

**Scope**: Minimal changes only. All existing files remain unchanged.

---

## 1. csproj Changes (`SafeZone.Server.csproj`)

Add Razor language support and implicit usings. Keep everything else the same.

Changes:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <!-- Keep existing PropertyGroup content -->
  
  <!-- ADD THIS (inside PropertyGroup): -->
  <RazorLangVersion>Latest</RazorLangVersion>
  
  <!-- Keep everything else -->
</Project>
```

**What changes**: Nothing functional - just enables Razor compilation.

---

## 2. Program.cs Changes

Add these **exactly in order**, **after** all existing service registrations but **before** `app.Run()`:

### Service Registration (after AddAuthorization, before builder.Build())
```csharp
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
```

### Middleware Pipeline (after UseStaticFiles, after UseAuthorization)
```csharp
// AFTER these existing lines:
// app.UseStaticFiles();
// app.UseRouting();
// app.UseCors(...);
// app.UseAuthentication();
// app.UseAuthorization();

// ADD these BEFORE the existing endpoint mapping:
app.MapBlazorHub();
```

### Important: Fallback Routing

We do NOT add `MapFallbackToPage("/_Host")` because:
- We want existing static HTML files in `wwwroot/` to always be served
- Blazor fallback would intercept routes that should return HTML files
- Without fallback, only explicit Blazor component routes will be served by Blazor

If Blazor pages are needed later, a `_Host.cshtml` can be added with `MapFallbackToPage("/_Host")` at that time.

---

## 3. Files Added (Minimal)

For Blazor Server to technically work, we need:

1. **`_Imports.razor`** - Razor component using directives (can be empty or minimal)
2. **`App.razor`** - Optional root component (not strictly needed without fallback)

But to keep it absolutely minimal:
- Just the csproj and Program.cs changes are enough to make this a "Blazor Server" project
- You could add `_Imports.razor` later if you actually want to write Blazor components

**Decision**: For this conversion ("just make it technically Blazor Server"), we only need:
1. csproj `<RazorLangVersion>` 
2. `builder.Services.AddRazorPages()` + `builder.Services.AddServerSideBlazor()`
3. `app.MapBlazorHub()`

That's it. Existing static files continue to be served. SignalR hubs (IncidentHub, AlertHub, MapHub) also continue to work.

---

## 4. Files NOT Changed

**All of these stay exactly as-is**:
- `wwwroot/**/*` (All HTML, CSS, JS files - 100% preserved)
- All Models/*.cs
- All Controllers/*.cs
- All Services/*.cs
- All DTOs/*.cs
- All Hubs/*.cs
- `Data/SafeZoneDbContext.cs`, `Data/SeedData.cs`
- `Program.cs` - only additions, no removals
- `appsettings.json`, `appsettings.Development.json`

---

## 5. Verification

After applying changes, verify:

1. Build succeeds (`dotnet build`)
2. Server runs (`dotnet run`)
3. All existing URLs work:
   - http://localhost:5000/ → Landing page (static HTML)
   - http://localhost:5000/login.html → Login page
   - http://localhost:5000/user/dashboard.html → User dashboard
   - http://localhost:5000/authority/dashboard.html → Authority dashboard
   - http://localhost:5000/swagger → Swagger UI for APIs

---

## Summary

| Change | Location | Purpose |
|--------|----------|---------|
| Add `RazorLangVersion` | .csproj | Enable Razor compilation |
| `AddRazorPages()` | Program.cs | Register Razor services |
| `AddServerSideBlazor()` | Program.cs | Register Blazor Server services |
| `MapBlazorHub()` | Program.cs | Map Blazor SignalR hub |

**No files are deleted or modified beyond these additions.**
