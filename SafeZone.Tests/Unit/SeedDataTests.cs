using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafeZone.Server.Data;
using SafeZone.Server.Models;
using Xunit;

namespace SafeZone.Tests.Unit;

public sealed class SeedDataTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;

    public SeedDataTests()
    {
        _connection.Open();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedData:SuperAdminPassword"] = "ConfiguredAdmin!2026"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<SafeZoneDbContext>(options => options.UseSqlite(_connection));
        services
            .AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SafeZoneDbContext>()
            .AddDefaultTokenProviders();

        _services = services.BuildServiceProvider();
        _services.GetRequiredService<SafeZoneDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task InitializeAsync_ConfiguredSuperAdminPassword_ResetsExistingSuperAdminOnly()
    {
        var userManager = _services.GetRequiredService<UserManager<User>>();
        var existingAdmin = new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin@safezone.pk",
            Email = "admin@safezone.pk",
            PhoneNumber = "+92511234567",
            FullName = "SafeZone Administrator",
            Role = UserRole.SuperAdmin,
            IsActive = true
        };
        var resident = new User
        {
            Id = Guid.NewGuid(),
            UserName = "resident@example.com",
            Email = "resident@example.com",
            PhoneNumber = "+923001112233",
            FullName = "Resident User",
            Role = UserRole.Resident,
            IsActive = true
        };

        Assert.True((await userManager.CreateAsync(existingAdmin, "OldAdmin!2026")).Succeeded);
        Assert.True((await userManager.CreateAsync(resident, "Resident!2026")).Succeeded);

        await SeedData.InitializeAsync(_services, isDevelopment: false);

        using var verificationScope = _services.CreateScope();
        var verificationUserManager = verificationScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var reloadedAdmin = await verificationUserManager.FindByIdAsync(existingAdmin.Id.ToString());
        var reloadedResident = await verificationUserManager.FindByIdAsync(resident.Id.ToString());

        Assert.NotNull(reloadedAdmin);
        Assert.NotNull(reloadedResident);
        Assert.True(await verificationUserManager.CheckPasswordAsync(reloadedAdmin!, "ConfiguredAdmin!2026"));
        Assert.False(await verificationUserManager.CheckPasswordAsync(reloadedAdmin!, "OldAdmin!2026"));
        Assert.True(await verificationUserManager.CheckPasswordAsync(reloadedResident!, "Resident!2026"));
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }
}
