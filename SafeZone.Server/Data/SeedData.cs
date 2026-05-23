using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Models;

namespace SafeZone.Server.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, bool ensureCreated = false)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SafeZoneDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (ensureCreated)
        {
            await context.Database.EnsureCreatedAsync();
        }

        await SeedRolesAsync(roleManager);
        await SeedCategoriesAsync(context);
        await SeedTestUsersAsync(userManager, roleManager, context);
        await SeedSampleIncidentsAsync(context, userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles = { "Resident", "Authority", "Admin", "SuperAdmin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task SeedCategoriesAsync(SafeZoneDbContext context)
    {
        if (await context.IncidentCategories.AnyAsync())
            return;

        var categories = new List<IncidentCategory>
        {
            new() { CategoryId = Guid.NewGuid(), Name = "Theft", Icon = "bag-steal", Color = "#FF3B5C", Description = "Theft or robbery incidents" },
            new() { CategoryId = Guid.NewGuid(), Name = "Assault", Icon = "person-danger", Color = "#FF6B35", Description = "Physical assault or attack" },
            new() { CategoryId = Guid.NewGuid(), Name = "Sexual Harassment", Icon = "warning-octagon", Color = "#C41E3A", Description = "Sexual harassment or abuse" },
            new() { CategoryId = Guid.NewGuid(), Name = "Robbery", Icon = "car-steal", Color = "#FF8C00", Description = "Armed robbery or mugging" },
            new() { CategoryId = Guid.NewGuid(), Name = "Accident", Icon = "car-crash", Color = "#4A90D9", Description = "Traffic or other accidents" },
            new() { CategoryId = Guid.NewGuid(), Name = "Fire", Icon = "flame", Color = "#FF4500", Description = "Fire incidents" },
            new() { CategoryId = Guid.NewGuid(), Name = "Medical Emergency", Icon = "heart-pulse", Color = "#32CD32", Description = "Medical emergencies" },
            new() { CategoryId = Guid.NewGuid(), Name = "Missing Person", Icon = "person-question", Color = "#9932CC", Description = "Missing person reports" },
            new() { CategoryId = Guid.NewGuid(), Name = "Suspicious Activity", Icon = "eye", Color = "#FFD700", Description = "Suspicious behavior or people" },
            new() { CategoryId = Guid.NewGuid(), Name = "Noise Complaint", Icon = "volume-3", Color = "#708090", Description = "Noise or disturbance" },
            new() { CategoryId = Guid.NewGuid(), Name = "Vandalism", Icon = "spray-can", Color = "#CD853F", Description = "Property damage or graffiti" },
            new() { CategoryId = Guid.NewGuid(), Name = "Traffic Violation", Icon = "car-taxi-front", Color = "#20B2AA", Description = "Traffic violations or reckless driving" },
            new() { CategoryId = Guid.NewGuid(), Name = "Curfew Violation", Icon = "moon-stars", Color = "#191970", Description = "Curfew violation reports" },
            new() { CategoryId = Guid.NewGuid(), Name = "Shooting", Icon = "crosshair", Color = "#8B0000", Description = "Gunfire or shooting incidents" },
            new() { CategoryId = Guid.NewGuid(), Name = "Other", Icon = "question", Color = "#696969", Description = "Other incident types" }
        };

        await context.IncidentCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedTestUsersAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, SafeZoneDbContext context)
    {
        var superAdmin = await EnsureTestUserAsync(userManager, "admin@safezone.pk", "+92511234567", "SafeZone Administrator", UserRole.SuperAdmin, "Admin123!", 5.0, 33.6844, 73.0479);
        if (!await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }

        var authorityUser = await EnsureTestUserAsync(userManager, "officer@safezone.pk", "+92511112233", "Inspector Ahmed Khan", UserRole.Authority, "Officer123!", 10.0, 33.6938, 73.0560);
        if (!await userManager.IsInRoleAsync(authorityUser, "Authority"))
        {
            await userManager.AddToRoleAsync(authorityUser, "Authority");
        }

        if (!await context.Authorities.AnyAsync(a => a.UserId == authorityUser.Id))
        {
            var authorityProfile = new Authority
            {
                AuthId = Guid.NewGuid(),
                UserId = authorityUser.Id,
                UnitName = "Islamabad Police - Blue Area Station",
                BadgeNumber = "ISB-7890",
                Type = AuthorityType.Police,
                JurisdictionCenterLat = 33.6938,
                JurisdictionCenterLng = 73.0560,
                JurisdictionGeoJson = null,
                ContactInfo = "Blue Area Police Station, Jinnah Avenue",
                EmergencyPhone = "15",
                IsOnDuty = true,
                Rank = "Inspector",
                Department = "Islamabad Capital Territory Police"
            };

            await context.Authorities.AddAsync(authorityProfile);
            await context.SaveChangesAsync();
        }

        var residentUser = await EnsureTestUserAsync(userManager, "user@safezone.pk", "+923001234567", "Ali Hassan", UserRole.Resident, "User123!", 2.0, 33.6650, 73.0770);
        if (!await userManager.IsInRoleAsync(residentUser, "Resident"))
        {
            await userManager.AddToRoleAsync(residentUser, "Resident");
        }

        await context.SaveChangesAsync();
    }

    private static async Task<User> EnsureTestUserAsync(
        UserManager<User> userManager,
        string userName,
        string phoneNumber,
        string fullName,
        UserRole role,
        string password,
        double radiusKm,
        double latitude,
        double longitude)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber || u.UserName == userName);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                PhoneNumber = phoneNumber,
                FullName = fullName,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                ProximityRadiusKm = radiusKm,
                LastKnownLatitude = latitude,
                LastKnownLongitude = longitude
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed {fullName}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            return user;
        }

        if (user.FullName != fullName || user.Role != role)
        {
            user.FullName = fullName;
            user.Role = role;
            user.IsActive = true;
            user.ProximityRadiusKm = radiusKm;
            user.LastKnownLatitude = latitude;
            user.LastKnownLongitude = longitude;

            await userManager.UpdateAsync(user);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, token, password);
            if (!resetResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to reset seeded password for {fullName}: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            }
        }

        return user;
    }

    private static async Task SeedSampleIncidentsAsync(SafeZoneDbContext context, UserManager<User> userManager)
    {
        if (await context.Incidents.AnyAsync())
            return;

        var residentUser = await userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == "+923001234567");

        var authorityUser = await userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == "+92511112233");

        var theftCategory = await context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Name == "Theft");
        var accidentCategory = await context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Name == "Accident");
        var suspiciousCategory = await context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Name == "Suspicious Activity");
        var assaultCategory = await context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Name == "Assault");
        var shootingCategory = await context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Name == "Shooting");

        var sampleIncidents = new List<Incident>
        {
            new()
            {
                IncidentId = Guid.NewGuid(),
                IncidentNumber = "INC-2026-0001",
                CategoryId = theftCategory?.CategoryId ?? Guid.Empty,
                ReporterId = residentUser?.Id,
                Latitude = 33.6844,
                Longitude = 73.0479,
                Address = "Centaurus Mall, Jinnah Avenue, Islamabad",
                Title = "Car Theft Reported in Parking Lot",
                Description = "My black Honda Civic (registration ISB-123) was stolen from the basement parking of Centaurus Mall. I parked it at 2:00 PM and when I returned at 5:30 PM, it was gone. Security cameras show a suspicious vehicle leaving at 4:15 PM.",
                Status = IncidentStatus.Pending,
                Severity = SeverityLevel.High,
                IsAnonymous = false,
                IsFIRFiled = false,
                EvidenceUrls = null,
                ReportedAt = DateTime.UtcNow.AddHours(-3),
                IncidentDateTime = DateTime.UtcNow.AddHours(-3.5),
                ResolvedAt = null,
                AssignedAuthorityId = null,
                AIGeneratedSummary = "Car theft reported at Centaurus Mall parking lot.",
                AICallLogId = null,
                WitnessCount = 2,
                SuspectDescription = "Unidentified individuals in a white sedan",
                EstimatedLoss = 3500000,
                SubCategory = "Vehicle Theft"
            },
            new()
            {
                IncidentId = Guid.NewGuid(),
                IncidentNumber = "INC-2026-0002",
                CategoryId = accidentCategory?.CategoryId ?? Guid.Empty,
                ReporterId = residentUser?.Id,
                Latitude = 33.6938,
                Longitude = 73.0560,
                Address = "F-7 Markaz, Jinnah Avenue, Islamabad",
                Title = "Traffic Accident at Intersection",
                Description = "A motorcycle and a car collided at the F-7 Markaz intersection. The motorcyclist has been taken to hospital. Both drivers were reportedly speeding.",
                Status = IncidentStatus.InProgress,
                Severity = SeverityLevel.Medium,
                IsAnonymous = false,
                IsFIRFiled = false,
                ReportedAt = DateTime.UtcNow.AddHours(-1),
                IncidentDateTime = DateTime.UtcNow.AddHours(-1.25),
                AssignedAuthorityId = authorityUser?.Id,
                AIGeneratedSummary = "Traffic accident involving motorcycle and car at F-7 Markaz.",
                WitnessCount = 5,
                SubCategory = "Traffic Collision"
            },
            new()
            {
                IncidentId = Guid.NewGuid(),
                IncidentNumber = "INC-2026-0003",
                CategoryId = suspiciousCategory?.CategoryId ?? Guid.Empty,
                ReporterId = null,
                Latitude = 33.6650,
                Longitude = 73.0770,
                Address = "G-11/3, Main Boulevard, Islamabad",
                Title = "Suspicious People Loitering",
                Description = "Two unidentified individuals have been loitering near the G-11 park entrance for the past hour. They appear to be watching houses in the neighborhood.",
                Status = IncidentStatus.Pending,
                Severity = SeverityLevel.Medium,
                IsAnonymous = true,
                IsFIRFiled = false,
                ReportedAt = DateTime.UtcNow.AddMinutes(-45),
                IncidentDateTime = DateTime.UtcNow.AddMinutes(-90),
                AIGeneratedSummary = "Suspicious individuals reported loitering in G-11 residential area.",
                SuspectDescription = "Two males in their late 20s, one wearing a black hoodie",
                SubCategory = "Suspicious Persons"
            },
            new()
            {
                IncidentId = Guid.NewGuid(),
                IncidentNumber = "INC-2026-0004",
                CategoryId = shootingCategory?.CategoryId ?? Guid.Empty,
                ReporterId = residentUser?.Id,
                Latitude = 33.7180,
                Longitude = 73.0790,
                Address = "Sector I-8 Markaz, Islamabad",
                Title = "Gunshots Heard in I-8",
                Description = "Multiple gunshots heard near I-8 Markaz around 9 PM. Neighbors report seeing a vehicle speeding away from the scene. Police have been notified.",
                Status = IncidentStatus.InProgress,
                Severity = SeverityLevel.Critical,
                IsAnonymous = true,
                IsFIRFiled = false,
                ReportedAt = DateTime.UtcNow.AddMinutes(-30),
                IncidentDateTime = DateTime.UtcNow.AddMinutes(-45),
                AIGeneratedSummary = "Gunfire reported in I-8 Markaz area. Investigation in progress.",
                WitnessCount = 3,
                SuspectDescription = "Black Vigo with tinted windows",
                SubCategory = "Gunfire"
            },
            new()
            {
                IncidentId = Guid.NewGuid(),
                IncidentNumber = "INC-2026-0005",
                CategoryId = assaultCategory?.CategoryId ?? Guid.Empty,
                ReporterId = residentUser?.Id,
                Latitude = 33.6750,
                Longitude = 73.0950,
                Address = "Rawalpindi Satellite Town, Islamabad",
                Title = "Street Robbery - Mobile Phone Snatching",
                Description = "Two individuals on a motorcycle snatched my mobile phone near Satellite Town market. They were armed with a knife. I am not injured but very shaken.",
                Status = IncidentStatus.Resolved,
                Severity = SeverityLevel.High,
                IsAnonymous = false,
                IsFIRFiled = true,
                ReportedAt = DateTime.UtcNow.AddDays(-2),
                IncidentDateTime = DateTime.UtcNow.AddDays(-2).AddHours(-2),
                ResolvedAt = DateTime.UtcNow.AddHours(-12),
                AIGeneratedSummary = "Street robbery incident - mobile phone snatched at knifepoint.",
                SuspectDescription = "Two males on a black Honda 125 motorcycle, faces covered",
                EstimatedLoss = 85000,
                SubCategory = "Street Crime"
            }
        };

        await context.Incidents.AddRangeAsync(sampleIncidents);
        await context.SaveChangesAsync();
    }
}
