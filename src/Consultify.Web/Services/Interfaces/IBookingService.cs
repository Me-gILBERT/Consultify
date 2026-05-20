using Consultify.Web.Models;

namespace Consultify.Web.Services.Interfaces;

public interface IBookingService
{
    Task<List<TimeSlot>> GetAvailableSlotsAsync(int consultantProfileId);
    Task<Appointment> BookSlotAsync(int timeSlotId, Guid customerUserId, string? notes);
    Task<Appointment?> CancelAppointmentAsync(int appointmentId, Guid userId, string? reason);
    bool CanCancel(Appointment appointment);
}
