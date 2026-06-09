using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;
using SafeZone.Server.Services;
using Xunit;

namespace SafeZone.Tests.Unit;

public class FirServiceTests : IDisposable
{
    private readonly SafeZoneDbContext _context;
    private readonly FirService _service;

    public FirServiceTests()
    {
        var options = new DbContextOptionsBuilder<SafeZoneDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new SafeZoneDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _service = new FirService(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateFirAsync_MissingReporter_RejectsExpiredSessionBeforeSaving()
    {
        _context.IncidentCategories.Add(new IncidentCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Other"
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFirAsync(CreateValidDto(), Guid.NewGuid()));

        Assert.Contains("sign in again", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await _context.Incidents.ToListAsync());
        Assert.Empty(await _context.FIRReports.ToListAsync());
    }

    [Fact]
    public async Task CreateFirAsync_MissingLinkedIncident_RejectsInvalidSelectionBeforeSaving()
    {
        var reporter = new User
        {
            Id = Guid.NewGuid(),
            UserName = "fir-reporter",
            FullName = "FIR Reporter"
        };
        _context.Users.Add(reporter);
        await _context.SaveChangesAsync();

        var dto = CreateValidDto() with { IncidentId = Guid.NewGuid() };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFirAsync(dto, reporter.Id));

        Assert.Contains("incident", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await _context.FIRReports.ToListAsync());
    }

    [Fact]
    public async Task CreateFirAsync_OverlongComplainantName_RejectsFormBeforeSaving()
    {
        var reporter = new User
        {
            Id = Guid.NewGuid(),
            UserName = "validation-reporter",
            FullName = "Validation Reporter"
        };
        _context.Users.Add(reporter);
        await _context.SaveChangesAsync();

        var dto = CreateValidDto() with { ComplainantName = new string('x', 101) };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFirAsync(dto, reporter.Id));

        Assert.Contains("ComplainantName", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await _context.Incidents.ToListAsync());
        Assert.Empty(await _context.FIRReports.ToListAsync());
    }

    [Fact]
    public async Task CreateFirAsync_ValidStandaloneFir_CreatesIncidentAndNormalizesDates()
    {
        var reporter = new User
        {
            Id = Guid.NewGuid(),
            UserName = "standalone-reporter",
            FullName = "Standalone Reporter"
        };
        _context.Users.Add(reporter);
        _context.IncidentCategories.Add(new IncidentCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Other"
        });
        await _context.SaveChangesAsync();

        var dto = CreateValidDto() with
        {
            IncidentDateTime = new DateTime(2026, 6, 9, 20, 30, 0, DateTimeKind.Unspecified),
            ComplainantDateOfBirth = new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var result = await _service.CreateFirAsync(dto, reporter.Id);

        Assert.Equal(DateTimeKind.Utc, result.IncidentDateTime.Kind);
        Assert.Equal(DateTimeKind.Utc, result.ComplainantDateOfBirth!.Value.Kind);
        Assert.Single(await _context.Incidents.ToListAsync());
        Assert.Single(await _context.FIRReports.ToListAsync());
    }

    private static CreateFirDto CreateValidDto() => new()
    {
        ComplainantName = "Test Complainant",
        ComplainantCNIC = "12345-1234567-1",
        IncidentNarrative = "A complete FIR narrative for persistence testing.",
        IncidentDateTime = DateTime.UtcNow,
        IncidentPlace = "Islamabad",
        IncidentLatitude = 33.6844,
        IncidentLongitude = 73.0479,
        NumberOfAccused = 1,
        DeclarationAccepted = true
    };
}
