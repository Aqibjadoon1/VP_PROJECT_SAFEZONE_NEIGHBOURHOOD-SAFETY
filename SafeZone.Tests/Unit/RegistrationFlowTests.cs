using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;
using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Unit;

public sealed class RegistrationFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SafeZoneDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;

    public RegistrationFlowTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SafeZoneDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SafeZoneDbContext(options);
        _context.Database.EnsureCreated();

        var userStore = new UserStore<User, IdentityRole<Guid>, SafeZoneDbContext, Guid>(_context);
        var roleStore = new RoleStore<IdentityRole<Guid>, SafeZoneDbContext, Guid>(_context);

        _userManager = new UserManager<User>(
            userStore,
            Options.Create(new IdentityOptions
            {
                Password =
                {
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireNonAlphanumeric = true,
                    RequiredLength = 8
                },
                User = { RequireUniqueEmail = false }
            }),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            null!);

        _roleManager = new RoleManager<IdentityRole<Guid>>(
            roleStore,
            Array.Empty<IRoleValidator<IdentityRole<Guid>>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "SafeZone",
                ["Jwt:Audience"] = "SafeZoneClient",
                ["Jwt:ExpiryMinutes"] = "15"
            })
            .Build();
    }

    [Fact]
    public async Task RegisterAsync_WhenJwtKeyIsMissing_StillCreatesAccountAndReturnsUserVisibleSuccess()
    {
        var service = new AuthService(_userManager, _roleManager, _context, _configuration);
        var dto = new RegisterDto
        {
            FullName = "Ayesha Khan",
            Email = "ayesha@example.com",
            PhoneNumber = "+923009991111",
            Password = "SafeZone!123",
            ConfirmPassword = "SafeZone!123",
            Role = UserRole.Resident
        };

        var result = await service.RegisterAsync(dto);

        Assert.True(result.Success);
        Assert.Contains("Account created", result.Message);
        Assert.Null(result.Token);
        Assert.Null(result.RefreshToken);

        var saved = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
        Assert.NotNull(saved);
        Assert.Equal(dto.Email, saved!.Email);
        Assert.True(await _userManager.IsInRoleAsync(saved, "Resident"));
    }

    public void Dispose()
    {
        _userManager.Dispose();
        _roleManager.Dispose();
        _context.Dispose();
        _connection.Dispose();
    }
}
