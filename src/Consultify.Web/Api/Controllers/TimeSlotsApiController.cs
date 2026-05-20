using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Api.ViewModels;

namespace Consultify.Web.Api.Controllers;

[Route("api/timeslots")]
[ApiController]
public class TimeSlotsApiController : ControllerBase
{
    private readonly IConsultantService _consultantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TimeSlotsApiController(IConsultantService consultantService, UserManager<ApplicationUser> userManager)
    {
        _consultantService = consultantService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SlotResponseDto>>>> GetAvailable([FromQuery] int consultantProfileId)
    {
        var slots = await _consultantService.GetTimeSlotsAsync(consultantProfileId);
        var available = slots
            .Where(s => !s.IsBooked && s.StartTime > DateTime.UtcNow)
            .OrderBy(s => s.StartTime)
            .Select(s => new SlotResponseDto
            {
                Id = s.Id,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsBooked = s.IsBooked
            })
            .ToList();

        return Ok(ApiResponse<List<SlotResponseDto>>.Ok(available));
    }

    [HttpPost]
    [Authorize(Roles = "Consultant")]
    public async Task<ActionResult<ApiResponse<List<SlotResponseDto>>>> Create([FromBody] SlotRequestDto request)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null)
            return BadRequest(ApiResponse<List<SlotResponseDto>>.Fail("Consultant profile not found."));

        if (profile.Id != request.ConsultantProfileId)
            return Forbid();

        try
        {
            List<TimeSlot> slots;
            if (request.AutoSplit)
            {
                slots = await _consultantService.CreateTimeSlotsAsync(
                    profile.Id, request.StartTime.Date,
                    request.StartTime.TimeOfDay, request.EndTime.TimeOfDay);
            }
            else
            {
                var slot = new TimeSlot
                {
                    ConsultantProfileId = profile.Id,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    IsBooked = false
                };
                _consultantService.GetType(); // placeholder — manual add
                return BadRequest(ApiResponse<List<SlotResponseDto>>.Fail("Single slot creation not implemented via API."));
            }

            var result = slots.Select(s => new SlotResponseDto
            {
                Id = s.Id,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsBooked = s.IsBooked
            }).ToList();

            return Ok(ApiResponse<List<SlotResponseDto>>.Ok(result, $"{result.Count} slot(s) created."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<SlotResponseDto>>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Consultant")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null)
            return BadRequest(ApiResponse<object>.Fail("Consultant profile not found."));

        var result = await _consultantService.RemoveTimeSlotAsync(id, profile.Id);
        if (!result)
            return BadRequest(ApiResponse<object>.Fail("Slot not found or is already booked."));

        return Ok(ApiResponse<object>.Ok(new { }, "Slot deleted."));
    }
}
