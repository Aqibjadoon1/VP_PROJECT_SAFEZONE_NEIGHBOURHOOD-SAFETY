using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public class FirService : IFirService
{
    private readonly SafeZoneDbContext _context;
    private readonly IGmailNotificationService? _gmail;
    private readonly ISlackNotificationService? _slack;
    private readonly ILogger<FirService>? _logger;
    private readonly string _baseUrl;

    public FirService(SafeZoneDbContext context, IGmailNotificationService? gmail = null, ISlackNotificationService? slack = null, ILogger<FirService>? logger = null, IConfiguration? configuration = null)
    {
        _context = context;
        _gmail = gmail;
        _slack = slack;
        _logger = logger;
        _baseUrl = configuration?["BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<FirResponseDto> CreateFirAsync(CreateFirDto dto, Guid reporterId)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        var incidentId = dto.IncidentId ?? Guid.Empty;
        if (incidentId == Guid.Empty)
        {
            var otherCategory = await _context.IncidentCategories.FirstOrDefaultAsync(c => c.Name == "Other")
                ?? await _context.IncidentCategories.FirstOrDefaultAsync();
            var categoryId = otherCategory?.CategoryId ?? Guid.Empty;

            var incident = new Incident
            {
                IncidentId = Guid.NewGuid(),
                IncidentNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                CategoryId = categoryId,
                Title = $"FIR: {dto.ComplainantName ?? "Unknown"}",
                Description = dto.IncidentNarrative ?? "FIR filed.",
                Severity = SeverityLevel.Medium,
                Status = IncidentStatus.Pending,
                ReporterId = reporterId,
                Latitude = dto.IncidentLatitude,
                Longitude = dto.IncidentLongitude,
                Address = dto.IncidentPlace,
                ReportedAt = DateTime.UtcNow,
                IsAnonymous = false
            };
            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();
            incidentId = incident.IncidentId;
        }

        var fir = new FIRReport
        {
            FIRId = Guid.NewGuid(),
            FIRNumber = await GenerateFirNumberAsync(),
            IncidentId = incidentId,
            ReporterId = reporterId,
            ComplainantName = dto!.ComplainantName!,
            ComplainantCNIC = dto.ComplainantCNIC,
            ComplainantPhone = dto.ComplainantPhone,
            ComplainantAddress = dto.ComplainantAddress,
            ComplainantFatherName = dto.ComplainantFatherName,
            ComplainantDateOfBirth = dto.ComplainantDateOfBirth,
            AccusedDescription = dto.AccusedDescription,
            IncidentNarrative = dto!.IncidentNarrative!,
            WitnessDetails = dto.WitnessDetails,
            PropertyLost = dto.PropertyLost,
            EstimatedLoss = dto.EstimatedLoss,
            Status = FIRStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            IncidentDateTime = dto.IncidentDateTime,
            IncidentPlace = dto.IncidentPlace,
            IncidentLatitude = dto.IncidentLatitude,
            IncidentLongitude = dto.IncidentLongitude,
            NumberOfAccused = dto.NumberOfAccused,
            AccusedKnown = dto.AccusedKnown,
            AccusedName = dto.AccusedName,
            AccusedCNIC = dto.AccusedCNIC,
            AccusedAddress = dto.AccusedAddress,
            DeclarationAccepted = dto.DeclarationAccepted
        };

        _context.FIRReports.Add(fir);
        await _context.SaveChangesAsync();

        var response = await MapToResponseAsync(fir);

        _ = Task.Run(async () =>
        {
            try
            {
                // Notify reporter
                var reporter = await _context.Users.FindAsync(reporterId);
                var recipientEmail = reporter?.Email ?? reporter?.UserName;
                if (!string.IsNullOrWhiteSpace(recipientEmail) && _gmail != null)
                {
                    var sent = await _gmail.SendEmailAsync(recipientEmail,
                        $"FIR {fir.FIRNumber} — Submitted Successfully",
                        $"Dear {reporter?.FullName ?? "User"},\n\nYour FIR #{fir.FIRNumber} has been submitted successfully and is pending review by the authorities.\n\nYou will be notified when your FIR status changes.\n\n— SafeZone Emergency System");

                    if (!sent)
                        _logger?.LogWarning("[FIR Notification] Gmail not sent to {Email}", recipientEmail);
                }

                // Notify all superadmins
                var superAdmins = await _context.Users
                    .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive)
                    .ToListAsync();

                foreach (var admin in superAdmins)
                {
                    var adminEmail = admin.Email ?? admin.UserName;
                    if (!string.IsNullOrWhiteSpace(adminEmail) && _gmail != null)
                    {
                        await _gmail.SendEmailAsync(adminEmail,
                            $"New FIR Filed: {fir.FIRNumber}",
                            $"A new FIR has been filed.\n\nFIR #: {fir.FIRNumber}\nComplainant: {fir.ComplainantName}\nIncident: {fir.IncidentNarrative?[..Math.Min(fir.IncidentNarrative.Length, 200)]}\nSubmitted: {fir.SubmittedAt:MMM dd, yyyy HH:mm}\n\nReview at: {_baseUrl}/authority/fir-management");
                    }
                }

                // Slack notification on FIR creation
                if (_slack != null)
                {
                    await _slack.SendAlertAsync(
                        $"New FIR Filed: {fir.FIRNumber}",
                        $"FIR #{fir.FIRNumber} has been filed by {fir.ComplainantName}. Incident: {(fir.IncidentNarrative?.Length > 100 ? fir.IncidentNarrative[..100] + "..." : fir.IncidentNarrative)}",
                        "Medium");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send FIR creation notifications.");
            }
        });

        return response;
    }

    public async Task<FirResponseDto?> GetFirByIdAsync(Guid firId)
    {
        var fir = await _context.FIRReports
            .AsNoTracking()
            .Include(f => f.Reporter)
            .Include(f => f.Incident)
            .FirstOrDefaultAsync(f => f.FIRId == firId);

        return fir == null ? null : await MapToResponseAsync(fir);
    }

    public async Task<List<FirListDto>> GetMyFirsAsync(Guid reporterId)
    {
        return await _context.FIRReports
            .AsNoTracking()
            .Include(f => f.Incident)
            .Include(f => f.ReviewedByAuthority)
            .Where(f => f.ReporterId == reporterId)
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => new FirListDto
            {
                FirId = f.FIRId,
                FirNumber = f.FIRNumber,
                IncidentId = f.IncidentId,
                IncidentTitle = f.Incident != null ? f.Incident.Title : null,
                ComplainantName = f.ComplainantName,
                Status = f.Status,
                SubmittedAt = f.SubmittedAt,
                ReviewedAt = f.ReviewedAt,
                ReviewedByName = f.ReviewedByAuthority != null ? f.ReviewedByAuthority.FullName : null
            })
            .ToListAsync();
    }

    public async Task<List<FirListDto>> GetAllFirsAsync(FIRStatus? status = null)
    {
        var query = _context.FIRReports
            .AsNoTracking()
            .Include(f => f.Incident)
            .Include(f => f.ReviewedByAuthority)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        return await query
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => new FirListDto
            {
                FirId = f.FIRId,
                FirNumber = f.FIRNumber,
                IncidentId = f.IncidentId,
                IncidentTitle = f.Incident != null ? f.Incident.Title : null,
                ComplainantName = f.ComplainantName,
                Status = f.Status,
                SubmittedAt = f.SubmittedAt,
                ReviewedAt = f.ReviewedAt,
                ReviewedByName = f.ReviewedByAuthority != null ? f.ReviewedByAuthority.FullName : null
            })
            .ToListAsync();
    }

    public async Task<FirResponseDto?> ReviewFirAsync(Guid firId, FIRStatus status, string? rejectionReason, Guid reviewerId)
    {
        var fir = await _context.FIRReports
            .FirstOrDefaultAsync(f => f.FIRId == firId);

        if (fir == null) return null;

        fir.Status = status;
        fir.ReviewedAt = DateTime.UtcNow;
        fir.ReviewedByAuthorityId = reviewerId;
        
        if (status == FIRStatus.Rejected && !string.IsNullOrEmpty(rejectionReason))
        {
            fir.RejectionReason = rejectionReason;
        }

        await _context.SaveChangesAsync();

        if (_gmail != null && (status == FIRStatus.Accepted || status == FIRStatus.Rejected))
        {
            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == fir.ReporterId);
            var recipientEmail = reporter?.Email ?? reporter?.UserName;
            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var sent = await _gmail.SendFirStatusEmailAsync(recipientEmail, fir.FIRNumber, status.ToString());
                        if (!sent)
                        {
                            _logger?.LogWarning("[FIR Notification] Gmail status email was not sent to {Email}. Check Gmail API configuration.", recipientEmail);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to send FIR status email to {Email}.", recipientEmail);
                    }
                });
            }
            else
            {
                _logger?.LogWarning("[FIR Notification] Cannot send email: reporter {UserId} has no email address.", fir.ReporterId);
            }
        }

        if (_slack != null && (status == FIRStatus.Accepted || status == FIRStatus.Rejected))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _slack.SendAlertAsync(
                        $"FIR {fir.FIRNumber} — {status}",
                        $"FIR #{fir.FIRNumber} has been {status.ToString().ToLowerInvariant()} by authority.",
                        status == FIRStatus.Rejected ? "High" : "Medium");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to send Slack FIR alert.");
                }
            });
        }

        return await MapToResponseAsync(fir);
    }

    public async Task<List<FirListDto>> GetFirsByStatusAsync(FIRStatus status)
    {
        return await GetAllFirsAsync(status);
    }

    private async Task<string> GenerateFirNumberAsync()
    {
        var timestamp = DateTime.UtcNow;
        var random = Random.Shared.Next(1000, 9999);
        var number = $"FIR-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{random}";

        // Ensure uniqueness by checking database
        var exists = await _context.FIRReports.AnyAsync(f => f.FIRNumber == number);
        if (exists)
        {
            random = Random.Shared.Next(1000, 9999);
            number = $"FIR-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{random}";
        }

        return number;
    }

    private async Task<FirResponseDto> MapToResponseAsync(FIRReport fir)
    {
        var reviewerName = fir.ReviewedByAuthorityId != null
            ? await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == fir.ReviewedByAuthorityId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync()
            : null;

        var incidentTitle = fir.IncidentId != Guid.Empty
            ? await _context.Incidents
                .AsNoTracking()
                .Where(i => i.IncidentId == fir.IncidentId)
                .Select(i => i.Title)
                .FirstOrDefaultAsync()
            : null;

        var reporterName = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == fir.ReporterId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        return new FirResponseDto
        {
            FirId = fir.FIRId,
            FirNumber = fir.FIRNumber,
            IncidentId = fir.IncidentId,
            IncidentTitle = incidentTitle,
            ReporterId = fir.ReporterId,
            ReporterName = reporterName,
            ComplainantName = fir.ComplainantName,
            ComplainantCNIC = fir.ComplainantCNIC,
            ComplainantPhone = fir.ComplainantPhone,
            ComplainantAddress = fir.ComplainantAddress,
            ComplainantFatherName = fir.ComplainantFatherName,
            ComplainantDateOfBirth = fir.ComplainantDateOfBirth,
            AccusedDescription = fir.AccusedDescription,
            IncidentNarrative = fir.IncidentNarrative,
            WitnessDetails = fir.WitnessDetails,
            PropertyLost = fir.PropertyLost,
            EstimatedLoss = fir.EstimatedLoss,
            Status = fir.Status,
            RejectionReason = fir.RejectionReason,
            SubmittedAt = fir.SubmittedAt,
            ReviewedAt = fir.ReviewedAt,
            ReviewedByAuthorityId = fir.ReviewedByAuthorityId,
            ReviewedByName = reviewerName,
            PdfUrl = fir.PDFUrl,
            IncidentDateTime = fir.IncidentDateTime,
            IncidentPlace = fir.IncidentPlace,
            IncidentLatitude = fir.IncidentLatitude,
            IncidentLongitude = fir.IncidentLongitude,
            NumberOfAccused = fir.NumberOfAccused,
            AccusedKnown = fir.AccusedKnown,
            AccusedName = fir.AccusedName,
            AccusedCNIC = fir.AccusedCNIC,
            AccusedAddress = fir.AccusedAddress,
            DeclarationAccepted = fir.DeclarationAccepted
        };
    }
}
