using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeZone.Server.DTOs;
using SafeZone.Server.Models;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FirController : ControllerBase
{
    private readonly IFirService _firService;

    public FirController(IFirService firService)
    {
        _firService = firService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost]
    public async Task<ActionResult<FirResponseDto>> CreateFir(CreateFirDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _firService.CreateFirAsync(dto, userId.Value);
        return CreatedAtAction(nameof(GetFir), new { id = result.FirId }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FirResponseDto>> GetFir(Guid id)
    {
        var fir = await _firService.GetFirByIdAsync(id);
        if (fir == null) return NotFound(new { message = "FIR not found" });

        var userId = GetCurrentUserId();
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (userRole != "Authority" && userRole != "SuperAdmin")
        {
            if (fir.ReporterId != userId)
            {
                return Forbid();
            }
        }

        return Ok(fir);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<FirListDto>>> GetMyFirs()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var firs = await _firService.GetMyFirsAsync(userId.Value);
        return Ok(firs);
    }

    [HttpGet]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<List<FirListDto>>> GetAllFirs([FromQuery] int? status)
    {
        FIRStatus? statusEnum = null;
        if (status.HasValue)
        {
            statusEnum = (FIRStatus)status.Value;
        }

        var firs = await _firService.GetAllFirsAsync(statusEnum);
        return Ok(firs);
    }

    [HttpPut("{id}/review")]
    [Authorize(Roles = "Authority,SuperAdmin")]
    public async Task<ActionResult<FirResponseDto>> ReviewFir(Guid id, ReviewFirDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _firService.ReviewFirAsync(id, dto.Status, dto.RejectionReason, userId.Value);
        if (result == null) return NotFound(new { message = "FIR not found" });

        return Ok(result);
    }
}
