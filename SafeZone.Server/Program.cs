using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Hubs;
using SafeZone.Server.Middleware;
using SafeZone.Server.Models;
using SafeZone.Server.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Keep startup logging on console so local runs do not require Windows Event Log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (builder.Environment.IsDevelopment())
{
    var configuredJwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(configuredJwtKey) || configuredJwtKey.Length < 32)
    {
        builder.Configuration["Jwt:Key"] = "SafeZoneDevelopmentJwtSigningKey-ReplaceBeforeDeploy-2026";
    }
}

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

var postgresConnectionString = ResolvePostgresConnectionString(builder.Configuration);
builder.Services.AddDbContext<SafeZoneDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgresConnectionString))
    {
        options.UseNpgsql(postgresConnectionString);
        return;
    }

    var sqliteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection")
        ?? "Data Source=SafeZone.db";
    options.UseSqlite(sqliteConnectionString);
});

// ── Identity Core ──────────────────────────────────────────────
builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<SafeZoneDbContext>()
.AddSignInManager()
.AddApiEndpoints();

// ── Authentication: Cookie (Blazor) + JWT (API) + Google OAuth ─
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "MultiAuth";
    options.DefaultAuthenticateScheme = "MultiAuth";
    options.DefaultChallengeScheme = "MultiAuth";
})
.AddPolicyScheme("MultiAuth", "MultiAuth", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/hubs") ||
            path.StartsWithSegments("/swagger"))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }
        return IdentityConstants.ApplicationScheme;
    };
})
.AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = false;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddCookie(IdentityConstants.ExternalScheme, options =>
{
    options.Cookie.Name = "SafeZone.External";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

    if (jwtKey.Length < 32)
        throw new InvalidOperationException("JWT Key must be at least 32 characters long.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/incidents") ||
                 path.StartsWithSegments("/hubs/alerts") ||
                 path.StartsWithSegments("/hubs/map") ||
                 path.StartsWithSegments("/hubs/calls")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.SaveTokens = false;
    });
}

builder.Services.AddAuthorization();

// ── Blazor Server ──────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();

// ── SignalR ────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Application Services ───────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IFirService, FirService>();
builder.Services.AddScoped<ISosService, SosService>();
builder.Services.AddScoped<ToastService>();

builder.Services.AddSingleton<ISpeechToText, MockSttService>();
builder.Services.AddSingleton<ILanguageModel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<GroqLlmService>>();
    var apiKey = config["Groq:ApiKey"];
    var modelName = config["Groq:ModelName"] ?? "llama-3.1-8b-instant";
    var endpoint = config["Groq:Endpoint"] ?? "https://api.groq.com/openai/v1";
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        logger.LogWarning("Groq:ApiKey is not configured. LLM service will run in mock/fallback mode.");
    }
    return new GroqLlmService(apiKey, modelName, endpoint, logger);
});
builder.Services.AddSingleton<ITextToSpeech, MockTtsService>();
builder.Services.AddSingleton<IVoicePipeline, VoicePipelineService>();
builder.Services.AddSingleton<IVoiceCallService, VoiceCallService>();
builder.Services.AddSingleton<ISmsService, MockSmsService>();
builder.Services.AddSingleton<IVoiceActivityDetector, EnergyVadService>();
builder.Services.AddSingleton<ISlackNotificationService, SlackNotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<IGmailNotificationService, GmailNotificationService>();
builder.Services.AddSingleton<IBlobStorageService, LocalBlobStorageService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithExposedHeaders("*");
    });

    options.AddPolicy("Production", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<SafeZoneDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    var providerName = dbContext.Database.ProviderName ?? string.Empty;

    if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogInformation("Using PostgreSQL database provider. Ensuring schema exists for production deployment.");
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        logger.LogInformation("Using SQLite database provider. Applying EF Core migrations.");
        await dbContext.Database.MigrateAsync();
    }

    await SeedData.InitializeAsync(services, app.Environment.IsDevelopment(), ensureCreated: false);
}

// Startup configuration validation
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var gmailClientId = config["Gmail:ClientId"];
    var slackWebhook = config["Slack:WebhookUrl"];
    var jwtKey = config["Jwt:Key"];

    if (string.IsNullOrWhiteSpace(gmailClientId))
    {
        logger.LogWarning(
            "[DEPLOY] Gmail API is NOT configured. Email notifications will not be sent. " +
            "Set environment variables: Gmail__ClientId, Gmail__ClientSecret, Gmail__RefreshToken, Gmail__FromEmail. " +
            "See DEPLOY.md for setup instructions.");
    }
    if (string.IsNullOrWhiteSpace(slackWebhook))
    {
        logger.LogWarning(
            "[DEPLOY] Slack webhook is NOT configured. Slack alerts will not be sent. " +
            "Set environment variable: Slack__WebhookUrl. " +
            "See DEPLOY.md for setup instructions.");
    }
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        logger.LogError(
            "[DEPLOY] JWT signing key is missing or too short. API authentication will fail. " +
            "Set a strong JWT key via environment variable: Jwt__Key (min 32 chars).");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"An unexpected error occurred.\"}");
        });
    });
    app.UseHsts();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SafeZone API v1"));
}

app.UseHttpsRedirection();
app.UseCors(app.Environment.IsDevelopment() ? "AllowLocalDev" : "Production");
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (RequiresNoStore(context.Request.Path))
    {
        ApplyNoStoreHeaders(context);
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapHub<SafeZone.Server.Hubs.IncidentHub>("/hubs/incidents");
app.MapHub<SafeZone.Server.Hubs.AlertHub>("/hubs/alerts");
app.MapHub<SafeZone.Server.Hubs.MapHub>("/hubs/map");
app.MapHub<SafeZone.Server.Hubs.CallHub>("/hubs/calls");
app.MapHealthChecks("/health");

// External login provider challenge endpoint (Google OAuth)
app.MapGet("/external-login", async (HttpContext context, string provider, string? returnUrl) =>
{
    if (!string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Redirect("/login?error=Provider+not+available");
    }

    var schemeProvider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
    var schemes = await schemeProvider.GetAllSchemesAsync();
    var hasGoogleScheme = schemes.Any(s => s.Name == GoogleDefaults.AuthenticationScheme);

    if (hasGoogleScheme)
    {
        var callback = "/external-login-callback";
        var safeReturnUrl = GetSafeLocalReturnUrl(returnUrl);
        var redirectUri = string.IsNullOrWhiteSpace(safeReturnUrl)
            ? callback
            : $"{callback}?returnUrl={Uri.EscapeDataString(safeReturnUrl)}";

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            new[] { GoogleDefaults.AuthenticationScheme });
    }

    if (app.Environment.IsDevelopment())
    {
        // Google not configured — use mock auth for development with a real user
        var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
        var mockUser = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "+923001234567");
        if (mockUser is null)
        {
            return Results.Redirect("/login?error=No+mock+user+available.+Run+seed+data+first.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, mockUser.Id.ToString()),
            new(ClaimTypes.Name, mockUser.FullName ?? mockUser.UserName ?? "Google User"),
            new(ClaimTypes.Role, mockUser.Role.ToString()),
            new("FullName", mockUser.FullName ?? ""),
            new(ClaimTypes.MobilePhone, mockUser.PhoneNumber ?? ""),
        };

        var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
        var principal = new ClaimsPrincipal(identity);
        await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
        return Results.Redirect(mockUser.Role == UserRole.Resident ? "/user/dashboard" : "/authority/board");
    }
    else
    {
        return Results.Redirect("/login?error=Google+login+is+not+configured.+Add+Google+ClientId+and+ClientSecret.");
    }
});

app.MapGet("/external-login-callback", async (HttpContext context, string? returnUrl) =>
{
    var externalAuth = await context.AuthenticateAsync(IdentityConstants.ExternalScheme);
    if (!externalAuth.Succeeded || externalAuth.Principal is null)
    {
        await context.SignOutAsync(IdentityConstants.ExternalScheme);
        return Results.Redirect("/login?error=Google+login+was+cancelled+or+failed.");
    }

    var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
    var roleManager = context.RequestServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var externalUser = externalAuth.Principal;
    var email = externalUser.FindFirstValue(ClaimTypes.Email);
    var providerKey = externalUser.FindFirstValue(ClaimTypes.NameIdentifier);
    var fullName = externalUser.FindFirstValue(ClaimTypes.Name)
        ?? externalUser.FindFirstValue("name")
        ?? email
        ?? "Google User";

    var user = !string.IsNullOrWhiteSpace(email)
        ? await userManager.Users.FirstOrDefaultAsync(u => u.Email == email)
        : null;

    if (user is null)
    {
        var userName = !string.IsNullOrWhiteSpace(email)
            ? email
            : $"google-{providerKey ?? Guid.NewGuid().ToString("N")}@safezone.local";

        user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            FullName = fullName,
            Role = UserRole.Resident,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ProximityRadiusKm = 2.0,
            LastKnownLatitude = 33.6844,
            LastKnownLongitude = 73.0479
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            await context.SignOutAsync(IdentityConstants.ExternalScheme);
            var message = Uri.EscapeDataString(string.Join(" ", createResult.Errors.Select(e => e.Description)));
            return Results.Redirect($"/login?error={message}");
        }

        if (!await roleManager.RoleExistsAsync("Resident"))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>("Resident"));
        }

        await userManager.AddToRoleAsync(user, "Resident");
    }

    var (principal, primaryRole) = await BuildSafeZonePrincipalAsync(user, userManager);
    await context.SignOutAsync(IdentityConstants.ExternalScheme);
    await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
    return Results.Redirect(GetDashboardUrlForRole(primaryRole));
});

// Clear auth cookies endpoint — called from login page to prevent auto-login
app.MapGet("/clear-auth", async (HttpContext context) =>
{
    ApplyNoStoreHeaders(context);
    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
    await context.SignOutAsync(IdentityConstants.ExternalScheme);
    DeleteAuthCookies(context);
    return Results.Redirect("/login");
});

// Blazor Server login handler — fresh HTTP context for setting auth cookies
app.MapPost("/blazor-login", async (HttpContext context, [FromForm] LoginDto dto) =>
{
    if (!context.Request.HasFormContentType)
    {
        return Results.Redirect("/login?error=Invalid+request");
    }

    var signInManager = context.RequestServices.GetRequiredService<SignInManager<User>>();
    var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    var identifier = dto.Identifier?.Trim() ?? string.Empty;
    logger.LogInformation("Login attempt for identifier: {Identifier}", identifier);

    var user = await userManager.Users
        .Where(u => u.PhoneNumber == identifier || u.Email == identifier || u.UserName == identifier)
        .FirstOrDefaultAsync();

    if (user is null)
    {
        logger.LogWarning("Login failed: user not found for identifier: {Identifier}", identifier);
        return Results.Redirect("/login?error=Invalid+credentials");
    }

    var result = await signInManager.PasswordSignInAsync(user, dto.Password, isPersistent: false, lockoutOnFailure: true);

    if (!result.Succeeded)
    {
        logger.LogWarning("Login failed for user {UserId}: {Result}", user.Id, result);
        if (result.IsLockedOut)
        {
            return Results.Redirect("/login?error=Account+locked+due+to+too+many+failed+attempts");
        }
        return Results.Redirect("/login?error=Invalid+credentials");
    }

    var (principal, primaryRole) = await BuildSafeZonePrincipalAsync(user, userManager);
    await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
    return Results.Redirect(GetDashboardUrlForRole(primaryRole));
}).DisableAntiforgery();

// Blazor Server register handler — fresh HTTP context for setting auth cookies
app.MapPost("/blazor-register", async (HttpContext context, [FromForm] RegisterDto dto) =>
{
    if (!context.Request.HasFormContentType)
    {
        return Results.Redirect("/register?error=Invalid+request");
    }

    var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
    var roleManager = context.RequestServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var phone = dto.PhoneNumber?.Trim() ?? string.Empty;
    var existingUser = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
    if (existingUser is not null)
    {
        return Results.Redirect("/register?error=Phone+number+already+registered");
    }

    var normalizedRole = NormalizeRole(dto.Role.ToString());
    var userRole = normalizedRole == "Authority" ? UserRole.Authority : UserRole.Resident;
    var user = new User
    {
        UserName = phone,
        PhoneNumber = phone,
        FullName = dto.FullName?.Trim() ?? string.Empty,
        Role = userRole,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        Email = dto.Email
    };

    var createResult = await userManager.CreateAsync(user, dto.Password);
    if (!createResult.Succeeded)
    {
        var errors = Uri.EscapeDataString(string.Join(" ", createResult.Errors.Select(e => e.Description)));
        return Results.Redirect($"/register?error={errors}");
    }

    var roleName = userRole == UserRole.Authority ? "Authority" : "Resident";
    if (!await roleManager.RoleExistsAsync(roleName))
    {
        await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
    }

    await userManager.AddToRoleAsync(user, roleName);

    var (principal, primaryRole) = await BuildSafeZonePrincipalAsync(user, userManager);
    await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
    return Results.Redirect(GetDashboardUrlForRole(primaryRole));
}).DisableAntiforgery();

// Blazor Server logout handler — fresh HTTP context for clearing auth cookies
app.MapGet("/blazor-logout", async (HttpContext context, string? expired) =>
{
    try
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        await context.SignOutAsync(IdentityConstants.ExternalScheme);
    }
    catch
    {
        // SignOut may fail if no user is authenticated — still redirect to login
    }

    // Clear the Identity cookie directly as a fallback
    DeleteAuthCookies(context);

    return Results.Redirect(expired == "true" ? "/login?expired=true" : "/login?loggedOut=true");
});

static bool RequiresNoStore(PathString path)
{
    return path.StartsWithSegments("/user")
        || path.StartsWithSegments("/authority")
        || path.StartsWithSegments("/blazor-logout")
        || path.StartsWithSegments("/clear-auth")
        || path.StartsWithSegments("/external-login")
        || path.StartsWithSegments("/external-login-callback");
}

static void ApplyNoStoreHeaders(HttpContext context)
{
    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
}

static void DeleteAuthCookies(HttpContext context)
{
    context.Response.Cookies.Delete(".AspNetCore.Identity.Application");
    context.Response.Cookies.Delete(".AspNetCore.Cookies");
    context.Response.Cookies.Delete("SafeZone.External");
    context.Response.Cookies.Delete("Identity.External");
}

static string NormalizeRole(string? role)
{
    return role?.Trim().ToLowerInvariant() switch
    {
        "resident" => "Resident",
        "authority" => "Authority",
        "admin" => "Admin",
        "superadmin" or "super_admin" or "super admin" => "SuperAdmin",
        _ => "Resident"
    };
}

static string GetDashboardUrlForRole(string role)
{
    return NormalizeRole(role) switch
    {
        "Authority" or "Admin" or "SuperAdmin" => "/authority/board",
        _ => "/user/dashboard"
    };
}

static string GetSafeLocalReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return string.Empty;
    }

    return Uri.TryCreate(returnUrl, UriKind.Relative, out _)
        && returnUrl.StartsWith("/")
        && !returnUrl.StartsWith("//")
            ? returnUrl
            : string.Empty;
}

static async Task<(ClaimsPrincipal Principal, string PrimaryRole)> BuildSafeZonePrincipalAsync(User user, UserManager<User> userManager)
{
    var identityRoles = await userManager.GetRolesAsync(user);
    var roles = identityRoles
        .Select(NormalizeRole)
        .Append(NormalizeRole(user.Role.ToString()))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    var primaryRole = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase) ? "SuperAdmin"
        : roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ? "Admin"
        : roles.Contains("Authority", StringComparer.OrdinalIgnoreCase) ? "Authority"
        : "Resident";

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.FullName ?? user.UserName ?? string.Empty),
        new("FullName", user.FullName ?? string.Empty),
        new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
        new("PrimaryRole", primaryRole)
    };

    claims.Add(new Claim(ClaimTypes.Role, primaryRole));
    foreach (var role in roles.Where(r => !string.Equals(r, primaryRole, StringComparison.OrdinalIgnoreCase)))
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
    return (new ClaimsPrincipal(identity), primaryRole);
}

static string? ResolvePostgresConnectionString(IConfiguration configuration)
{
    var configuredConnectionString = configuration.GetConnectionString("PostgresConnection");
    if (!string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        return NormalizePostgresConnectionString(configuredConnectionString);
    }

    var databaseUrl = configuration["DATABASE_URL"];
    return string.IsNullOrWhiteSpace(databaseUrl)
        ? null
        : NormalizePostgresConnectionString(databaseUrl);
}

static string NormalizePostgresConnectionString(string connectionString)
{
    var trimmed = connectionString.Trim();
    if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
        (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
    {
        return trimmed;
    }

    var credentials = uri.UserInfo.Split(':', 2);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = credentials.Length > 0 ? Uri.UnescapeDataString(credentials[0]) : string.Empty,
        Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
        SslMode = SslMode.Require
    };

    foreach (var parameter in QueryHelpers.ParseQuery(uri.Query))
    {
        var value = parameter.Value.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            continue;
        }

        builder[NormalizePostgresUrlQueryKey(parameter.Key)] = value;
    }

    return builder.ConnectionString;
}

static string NormalizePostgresUrlQueryKey(string key)
{
    return key.Trim().ToLowerInvariant() switch
    {
        "sslmode" => "Ssl Mode",
        "sslcert" => "SSL Certificate",
        "sslkey" => "SSL Key",
        "sslrootcert" => "Root Certificate",
        "channel_binding" => "Channel Binding",
        "connect_timeout" => "Timeout",
        "application_name" => "Application Name",
        _ => key
    };
}

app.MapFallbackToPage("/_Host");

app.Run();
