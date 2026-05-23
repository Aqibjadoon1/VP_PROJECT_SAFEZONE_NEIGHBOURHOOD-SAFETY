using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public interface IFirPdfService
{
    Task<byte[]> GenerateFirPdfAsync(FIRReport firReport, Incident incident, string reporterName, string? authorityName = null);
}
