using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SafeZone.Server.Data;
using SafeZone.Server.Models;
using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
    private readonly Mock<SafeZoneDbContext> _contextMock;
    private readonly Mock<IConfiguration> _configMock;

    public AuthServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        _contextMock = new Mock<SafeZoneDbContext>(new DbContextOptions<SafeZoneDbContext>());
        _configMock = new Mock<IConfiguration>();
    }

    private AuthService CreateService() => new(
        _userManagerMock.Object,
        _roleManagerMock.Object,
        _contextMock.Object,
        _configMock.Object);

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var service = CreateService();
        var token = service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateRefreshToken_IsAtLeast64Chars()
    {
        var service = CreateService();
        var token = service.GenerateRefreshToken();

        Assert.True(token.Length >= 64);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var service = CreateService();
        var t1 = service.GenerateRefreshToken();
        var t2 = service.GenerateRefreshToken();

        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public void GenerateRefreshToken_IsBase64String()
    {
        var service = CreateService();
        var token = service.GenerateRefreshToken();

        Assert.NotNull(Convert.FromBase64String(token));
    }
}
