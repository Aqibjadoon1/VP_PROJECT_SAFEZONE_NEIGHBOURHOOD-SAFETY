using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Services;

public class GroqLlmServiceTests
{
    [Fact]
    public void Constructor_WithApiKey_SetsIsMockFalse()
    {
        using var service = new GroqLlmService(apiKey: "test-key-123");
        Assert.False(service.IsMock);
    }

    [Fact]
    public void Constructor_WithoutApiKey_SetsIsMockTrue()
    {
        using var service = new GroqLlmService(apiKey: null);
        Assert.True(service.IsMock);
    }

    [Fact]
    public void Constructor_EmptyApiKey_SetsIsMockTrue()
    {
        using var service = new GroqLlmService(apiKey: "");
        Assert.True(service.IsMock);
    }

    [Fact]
    public async Task GenerateResponseAsync_NotConfigured_ReturnsFallback()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("hello test");
        Assert.NotNull(response);
        Assert.NotEmpty(response);
    }

    [Fact]
    public async Task GenerateResponseAsync_EmergencyKeywords_ReturnsEmergencyResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("I need emergency help SOS");
        Assert.Contains("emergency", response.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateResponseAsync_FireKeywords_ReturnsFireResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("There is a fire in the building");
        Assert.Contains("fire", response.ToLowerInvariant());
        Assert.Contains("evacuate", response.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateResponseAsync_MedicalKeywords_ReturnsMedicalResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("Someone is hurt need medical ambulance");
        Assert.Contains("medical", response.ToLowerInvariant());
        Assert.Contains("ambulance", response.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateResponseAsync_PoliceKeywords_ReturnsPoliceResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("Call the police there is a threat");
        Assert.Contains("police", response.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateResponseAsync_LocationKeywords_ReturnsLocationResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("What is my location?");
        Assert.Contains("location", response.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateResponseAsync_NullInput_ReturnsDefaultResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync(null!);
        Assert.Contains("How can I help", response);
    }

    [Fact]
    public async Task GenerateResponseAsync_EmptyInput_ReturnsDefaultResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("");
        Assert.Contains("How can I help", response);
    }

    [Fact]
    public async Task GenerateResponseAsync_UnknownInput_ReturnsGenericResponse()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync("random gibberish xyz123");
        Assert.Contains("Thank you", response);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithSystemPrompt_PassesPrompt()
    {
        using var service = new GroqLlmService(apiKey: null);
        var response = await service.GenerateResponseAsync(
            "fire",
            systemPrompt: "You are an emergency dispatcher");
        Assert.Contains("fire", response.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateResponseAsync_WithConversationHistory_DoesNotThrow()
    {
        using var service = new GroqLlmService(apiKey: null);
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi, how can I help?")
        };
        var response = await service.GenerateResponseAsync(
            "fire in kitchen",
            conversationHistory: history);
        Assert.Contains("fire", response.ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateResponseAsync_WithCancellationToken_ReturnsImmediately()
    {
        using var service = new GroqLlmService(apiKey: null);
        using var cts = new CancellationTokenSource();
        var response = await service.GenerateResponseAsync(
            "test",
            cancellationToken: cts.Token);
        Assert.NotNull(response);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_NoException()
    {
        var service = new GroqLlmService(apiKey: null);
        service.Dispose();
        service.Dispose();
        Assert.True(true);
    }
}
