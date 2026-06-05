using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Unit;

public class AuthServiceTests
{
    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var service = new AuthService(null!, null!, null!, null!);
        var token = service.GenerateRefreshToken();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateRefreshToken_IsAtLeast64Chars()
    {
        var service = new AuthService(null!, null!, null!, null!);
        var token = service.GenerateRefreshToken();
        Assert.True(token.Length >= 64);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var service = new AuthService(null!, null!, null!, null!);
        var t1 = service.GenerateRefreshToken();
        var t2 = service.GenerateRefreshToken();
        Assert.NotEqual(t1, t2);
    }
}
