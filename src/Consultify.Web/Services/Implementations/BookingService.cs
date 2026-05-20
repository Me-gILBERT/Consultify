using Microsoft.EntityFrameworkCore;
using Npgsql;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TimeSlot>> GetAvailableSlotsAsync(int consultantProfileId)
    {
        return await _context.TimeSlots
            .Where(ts => ts.ConsultantProfileId == consultantProfileId && !ts.IsBooked && ts.StartTime > DateTime.UtcNow)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();
    }

    public async Task<Appointment> BookSlotAsync(int timeSlotId, Guid customerUserId, string? notes)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var slot = await _context.TimeSlots
                .FirstOrDefaultAsync(s => s.Id == timeSlotId);

            if (slot == null || slot.IsBooked)
                throw new InvalidOperationException("This slot is no longer available.");

            slot.IsBooked = true;

            var appointment = new Appointment
            {
                TimeSlotId = slot.Id,
                CustomerUserId = customerUserId,
                ConsultantProfileId = slot.ConsultantProfileId,
                Status = AppointmentStatus.Scheduled,
                BookedAt = DateTime.UtcNow,
                Notes = notes
            };

            _context.Appointments.Add(appointment);

            try
            {
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return appointment;
            }
            catch (Exception ex) when (ex is DbUpdateConcurrencyException ||
                (ex is DbUpdateException due && due.InnerException is PostgresException pg && pg.SqlState == "40001"))
            {
                await tx.RollbackAsync();
                throw new InvalidOperationException("This slot was just booked by someone else. Please choose another.");
            }
        });
    }

    public async Task<Appointment?> CancelAppointmentAsync(int appointmentId, Guid userId, string? reason)
    {
        var appointment = await _context.Appointments
            .Include(a => a.TimeSlot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return null;

        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled appointments can be cancelled.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancellationReason = reason;

        appointment.TimeSlot.IsBooked = false;

        await _context.SaveChangesAsync();
        return appointment;
    }

    public bool CanCancel(Appointment appointment)
    {
        return appointment.Status == AppointmentStatus.Scheduled
            && appointment.TimeSlot.StartTime > DateTime.UtcNow.AddHours(24);
    }
}
