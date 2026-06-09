using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;
using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Unit;

public class IncidentServiceTests : IDisposable
{
    private readonly SafeZoneDbContext _context;
    private readonly IncidentService _service;

    public IncidentServiceTests()
    {
        var options = new DbContextOptionsBuilder<SafeZoneDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new SafeZoneDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _service = new IncidentService(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public void IncidentStatusEnum_HasAllValues()
    {
        var values = Enum.GetValues<IncidentStatus>();
        Assert.Equal(5, values.Length);
    }

    [Fact]
    public void SeverityLevelEnum_HasAllValues()
    {
        var values = Enum.GetValues<SeverityLevel>();
        Assert.Equal(4, values.Length);
    }

    [Fact]
    public void Incident_Creation_SetsDefaults()
    {
        var incident = new Incident
        {
            Title = "Test",
            Description = "Test desc"
        };

        Assert.Equal("Test", incident.Title);
        Assert.Equal(IncidentStatus.Pending, incident.Status);
    }

    [Fact]
    public async Task GetIncidentByIdAsync_ExistingIncident_ReturnsDto()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "test", FullName = "Test User" };
        var category = new IncidentCategory { CategoryId = Guid.NewGuid(), Name = "Theft", Icon = "🚨" };
        var incident = new Incident
        {
            IncidentId = Guid.NewGuid(),
            Title = "Test Incident",
            Description = "Test Description",
            Status = IncidentStatus.Pending,
            Severity = SeverityLevel.High,
            Category = category,
            Reporter = user,
            Latitude = 33.0,
            Longitude = 73.0,
            ReportedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.IncidentCategories.Add(category);
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        var result = await _service.GetIncidentByIdAsync(incident.IncidentId);

        Assert.NotNull(result);
        Assert.Equal("Test Incident", result!.Title);
        Assert.Equal("Theft", result.CategoryName);
        Assert.Equal("🚨", result.CategoryIcon);
    }

    [Fact]
    public async Task GetIncidentByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetIncidentByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateIncidentAsync_ValidDto_CreatesIncident()
    {
        var category = new IncidentCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "TestCategory",
            Icon = "🔥",
            Color = "#ff0000"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            FullName = "Test User",
            PhoneNumber = "+923001234567"
        };
        _context.IncidentCategories.Add(category);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new CreateIncidentDto
        {
            Title = "New Incident",
            Description = "Details",
            CategoryId = category.CategoryId,
            Severity = SeverityLevel.Medium,
            Latitude = 33.5,
            Longitude = 73.5
        };

        var result = await _service.CreateIncidentAsync(dto, user.Id);

        Assert.NotNull(result);
        Assert.Equal("New Incident", result.Title);
        Assert.Equal(IncidentStatus.Pending, result.Status);

        var saved = await _context.Incidents.FindAsync(result.IncidentId);
        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved!.ReporterId);
    }

    [Fact]
    public async Task CreateIncidentAsync_AnonymousDto_KeepsReporterIdForMyIncidents()
    {
        var category = new IncidentCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "AnonymousCategory",
            Icon = "eye",
            Color = "#00ff88"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "anonymous-reporter",
            FullName = "Anonymous Reporter",
            PhoneNumber = "+923001112222"
        };
        _context.IncidentCategories.Add(category);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new CreateIncidentDto
        {
            Title = "Anonymous Incident",
            Description = "Details",
            CategoryId = category.CategoryId,
            Severity = SeverityLevel.Medium,
            Latitude = 33.5,
            Longitude = 73.5,
            IsAnonymous = true
        };

        var result = await _service.CreateIncidentAsync(dto, user.Id);
        var myIncidents = await _service.GetMyIncidentsAsync(user.Id);
        var detail = await _service.GetIncidentByIdAsync(result.IncidentId);

        Assert.Contains(myIncidents, incident => incident.IncidentId == result.IncidentId);
        Assert.NotNull(detail);
        Assert.Equal(user.Id, detail!.ReporterId);
        Assert.Equal("Anonymous", detail.ReporterName);
    }

    [Fact]
    public async Task CreateIncidentAsync_MissingReporter_RejectsExpiredSessionBeforeSaving()
    {
        var category = new IncidentCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "SessionValidationCategory",
            Icon = "warning",
            Color = "#ffb800"
        };
        _context.IncidentCategories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CreateIncidentDto
        {
            Title = "Expired session incident",
            Description = "This should not reach the database insert.",
            CategoryId = category.CategoryId,
            Severity = SeverityLevel.Medium,
            Latitude = 33.5,
            Longitude = 73.5
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateIncidentAsync(dto, Guid.NewGuid()));

        Assert.Contains("sign in again", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await _context.Incidents.ToListAsync());
    }

    [Fact]
    public async Task CreateIncidentAsync_OverlongTitle_RejectsFormBeforeSaving()
    {
        var category = new IncidentCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "ValidationCategory"
        };
        _context.IncidentCategories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CreateIncidentDto
        {
            Title = new string('x', 101),
            Description = "Valid description",
            CategoryId = category.CategoryId,
            Latitude = 33.5,
            Longitude = 73.5
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateIncidentAsync(dto, null));

        Assert.Contains("Title", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await _context.Incidents.ToListAsync());
    }

    [Fact]
    public async Task CreateIncidentAsync_UnspecifiedIncidentTime_NormalizesToUtc()
    {
        var category = new IncidentCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "UtcCategory"
        };
        _context.IncidentCategories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CreateIncidentDto
        {
            Title = "UTC incident",
            Description = "Timestamp normalization test",
            CategoryId = category.CategoryId,
            Latitude = 33.5,
            Longitude = 73.5,
            IncidentDateTime = new DateTime(2026, 6, 9, 20, 30, 0, DateTimeKind.Unspecified)
        };

        var result = await _service.CreateIncidentAsync(dto, null);

        Assert.Equal(DateTimeKind.Utc, result.IncidentDateTime!.Value.Kind);
    }
}
