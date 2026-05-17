using SafeZone.Server.DTOs;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public interface IFirService
{
    Task<FirResponseDto> CreateFirAsync(CreateFirDto dto, Guid reporterId);
    Task<FirResponseDto?> GetFirByIdAsync(Guid firId);
    Task<List<FirListDto>> GetMyFirsAsync(Guid reporterId);
    Task<List<FirListDto>> GetAllFirsAsync(FIRStatus? status = null);
    Task<FirResponseDto?> ReviewFirAsync(Guid firId, FIRStatus status, string? rejectionReason, Guid reviewerId);
    Task<List<FirListDto>> GetFirsByStatusAsync(FIRStatus status);
}
