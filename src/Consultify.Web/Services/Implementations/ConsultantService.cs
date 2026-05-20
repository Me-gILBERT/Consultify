using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Services.Implementations;

public class ConsultantService : IConsultantService
{
    private readonly ApplicationDbContext _context;

    public ConsultantService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ConsultantProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _context.ConsultantProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.UserId == userId);
    }

    public async Task<ConsultantProfile?> GetProfileByIdAsync(int profileId)
    {
        return await _context.ConsultantProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Id == profileId);
    }

    public async Task UpdateProfileAsync(ConsultantProfile profile)
    {
        _context.ConsultantProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TimeSlot>> GetTimeSlotsAsync(int consultantProfileId)
    {
        return await _context.TimeSlots
            .Where(ts => ts.ConsultantProfileId == consultantProfileId)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();
    }

    public async Task<List<TimeSlot>> CreateTimeSlotsAsync(int consultantProfileId, DateTime date, TimeSpan start, TimeSpan end)
    {
        if (start >= end)
            throw new ArgumentException("Start time must be before end time.");

        if (date.Kind != DateTimeKind.Utc)
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

        if (date.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Cannot create slots in the past.");

        var existingSlots = await _context.TimeSlots
            .Where(ts => ts.ConsultantProfileId == consultantProfileId
                && ts.StartTime.Date == date.Date)
            .ToListAsync();

        var newSlots = new List<TimeSlot>();
        var current = DateTime.SpecifyKind(date.Date.Add(start), DateTimeKind.Utc);

        while (current.AddMinutes(30) <= DateTime.SpecifyKind(date.Date.Add(end), DateTimeKind.Utc))
        {
            var slotStart = current;
            var slotEnd = current.AddMinutes(30);

            if (slotStart > DateTime.UtcNow && !existingSlots.Any(s => s.StartTime == slotStart))
            {
                newSlots.Add(new TimeSlot
                {
                    ConsultantProfileId = consultantProfileId,
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    IsBooked = false
                });
            }

            current = slotEnd;
        }

        if (newSlots.Count == 0)
            throw new InvalidOperationException("No new slots to create. They may already exist or are in the past.");

        _context.TimeSlots.AddRange(newSlots);
        await _context.SaveChangesAsync();
        return newSlots;
    }

    public async Task<List<TimeSlot>> CreateBulkTimeSlotsAsync(int consultantProfileId, DateOnly from, DateOnly to, TimeSpan start, TimeSpan end)
    {
        var allSlots = new List<TimeSlot>();
        var current = from;

        while (current <= to)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                var date = current.ToDateTime(TimeOnly.MinValue);
                var slots = await CreateTimeSlotsAsync(consultantProfileId, date, start, end);
                allSlots.AddRange(slots);
            }
            current = current.AddDays(1);
        }

        return allSlots;
    }

    public async Task<bool> RemoveTimeSlotAsync(int slotId, int consultantProfileId)
    {
        var slot = await _context.TimeSlots
            .FirstOrDefaultAsync(ts => ts.Id == slotId && ts.ConsultantProfileId == consultantProfileId);

        if (slot == null || slot.IsBooked)
            return false;

        _context.TimeSlots.Remove(slot);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Appointment>> GetAppointmentsAsync(int consultantProfileId, AppointmentStatus? status)
    {
        var query = _context.Appointments
            .Include(a => a.TimeSlot)
            .Include(a => a.CustomerUser)
            .Include(a => a.Review)
            .Where(a => a.ConsultantProfileId == consultantProfileId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.TimeSlot.StartTime)
            .ToListAsync();
    }

    public async Task<bool> MarkCompletedAsync(int appointmentId, int consultantProfileId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ConsultantProfileId == consultantProfileId);

        if (appointment == null || appointment.Status != AppointmentStatus.Scheduled)
            return false;

        appointment.Status = AppointmentStatus.Completed;
        await _context.SaveChangesAsync();
        return true;
    }
}
