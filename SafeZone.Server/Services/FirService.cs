using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Data;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public class FirService : IFirService
{
    private readonly SafeZoneDbContext _context;
    private readonly IGmailNotificationService? _gmail;

    public FirService(SafeZoneDbContext context, IGmailNotificationService? gmail = null)
    {
        _context = context;
        _gmail = gmail;
    }

    public async Task<FirResponseDto> CreateFirAsync(CreateFirDto dto, Guid reporterId)
    {
        var fir = new FIRReport
        {
            FIRId = Guid.NewGuid(),
            FIRNumber = GenerateFirNumber(),
            IncidentId = dto.IncidentId ?? Guid.Empty,
            ReporterId = reporterId,
            ComplainantName = dto.ComplainantName,
            ComplainantCNIC = dto.ComplainantCNIC,
            ComplainantPhone = dto.ComplainantPhone,
            ComplainantAddress = dto.ComplainantAddress,
            ComplainantFatherName = dto.ComplainantFatherName,
            ComplainantDateOfBirth = dto.ComplainantDateOfBirth,
            AccusedDescription = dto.AccusedDescription,
            IncidentNarrative = dto.IncidentNarrative,
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

        return await MapToResponseAsync(fir);
    }

    public async Task<FirResponseDto?> GetFirByIdAsync(Guid firId)
    {
        var fir = await _context.FIRReports
            .Include(f => f.Reporter)
            .Include(f => f.Incident)
            .FirstOrDefaultAsync(f => f.FIRId == firId);

        return fir == null ? null : await MapToResponseAsync(fir);
    }

     public async Task<List<FirListDto>> GetMyFirsAsync(Guid reporterId)
    {
        var firs = await _context.FIRReports
            .Include(f => f.Incident)
            .Include(f => f.ReviewedByAuthority)
            .Where(f => f.ReporterId == reporterId)
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();

        return firs.Select(f => new FirListDto
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
        }).ToList();
    }

     public async Task<List<FirListDto>> GetAllFirsAsync(FIRStatus? status = null)
    {
        var query = _context.FIRReports
            .Include(f => f.Incident)
            .Include(f => f.ReviewedByAuthority)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        var firs = await query
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();

        return firs.Select(f => new FirListDto
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
        }).ToList();
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
            if (reporter?.PhoneNumber != null)
            {
                _ = _gmail.SendFirStatusEmailAsync(reporter.PhoneNumber, fir.FIRNumber, status.ToString());
            }
        }

        return await MapToResponseAsync(fir);
    }

    public async Task<List<FirListDto>> GetFirsByStatusAsync(FIRStatus status)
    {
        return await GetAllFirsAsync(status);
    }

    private string GenerateFirNumber()
    {
        var timestamp = DateTime.UtcNow;
        var random = new Random().Next(1000, 9999);
        return $"FIR-{timestamp:yyyyMMdd}-{random}";
    }

    private async Task<FirResponseDto> MapToResponseAsync(FIRReport fir)
    {
        var reviewerName = fir.ReviewedByAuthorityId != null
            ? await _context.Users
                .Where(u => u.Id == fir.ReviewedByAuthorityId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync()
            : null;

        var incidentTitle = fir.IncidentId != Guid.Empty
            ? await _context.Incidents
                .Where(i => i.IncidentId == fir.IncidentId)
                .Select(i => i.Title)
                .FirstOrDefaultAsync()
            : null;

        var reporterName = await _context.Users
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
