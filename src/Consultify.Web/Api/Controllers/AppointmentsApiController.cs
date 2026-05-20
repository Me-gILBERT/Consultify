using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Api.ViewModels;

namespace Consultify.Web.Api.Controllers;

[Route("api/appointments")]
[ApiController]
public class AppointmentsApiController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppointmentsApiController(IBookingService bookingService, UserManager<ApplicationUser> userManager)
    {
        _bookingService = bookingService;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<AppointmentResponseDto>>>> GetAll([FromQuery] string? role, [FromQuery] string? status)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

        var query = context.Appointments
            .Include(a => a.TimeSlot)
            .Include(a => a.CustomerUser)
            .Include(a => a.ConsultantProfile).ThenInclude(cp => cp.User)
            .AsQueryable();

        if (role == "customer")
            query = query.Where(a => a.CustomerUserId == Guid.Parse(userId));
        else if (role == "consultant")
        {
            var profile = await context.ConsultantProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == Guid.Parse(userId));
            if (profile != null)
                query = query.Where(a => a.ConsultantProfileId == profile.Id);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
            query = query.Where(a => a.Status == statusEnum);

        var appointments = await query
            .OrderByDescending(a => a.TimeSlot.StartTime)
            .Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                ConsultantName = a.ConsultantProfile.User.FullName,
                CustomerName = a.CustomerUser.FullName,
                StartTime = a.TimeSlot.StartTime,
                EndTime = a.TimeSlot.EndTime,
                Status = a.Status.ToString(),
                Notes = a.Notes
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<AppointmentResponseDto>>> Book([FromBody] BookingRequestDto request)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        try
        {
            var appointment = await _bookingService.BookSlotAsync(request.TimeSlotId, Guid.Parse(userId), request.Notes);

            var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var full = await context.Appointments
                .Include(a => a.TimeSlot)
                .Include(a => a.CustomerUser)
                .Include(a => a.ConsultantProfile).ThenInclude(cp => cp.User)
                .FirstAsync(a => a.Id == appointment.Id);

            var dto = new AppointmentResponseDto
            {
                Id = full.Id,
                ConsultantName = full.ConsultantProfile.User.FullName,
                CustomerName = full.CustomerUser.FullName,
                StartTime = full.TimeSlot.StartTime,
                EndTime = full.TimeSlot.EndTime,
                Status = full.Status.ToString(),
                Notes = full.Notes
            };

            return Ok(ApiResponse<AppointmentResponseDto>.Ok(dto, "Appointment booked."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AppointmentResponseDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id}/cancel")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(int id, [FromBody] string? reason)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        try
        {
            var appointment = await _bookingService.CancelAppointmentAsync(id, Guid.Parse(userId), reason);
            return Ok(ApiResponse<object>.Ok(new { }, "Appointment cancelled."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
