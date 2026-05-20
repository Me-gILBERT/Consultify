using Consultify.Web.Models;

namespace Consultify.Web.Services.Interfaces;

public interface IConsultantService
{
    Task<ConsultantProfile?> GetProfileByUserIdAsync(Guid userId);
    Task<ConsultantProfile?> GetProfileByIdAsync(int profileId);
    Task UpdateProfileAsync(ConsultantProfile profile);
    Task<List<TimeSlot>> GetTimeSlotsAsync(int consultantProfileId);
    Task<List<TimeSlot>> CreateTimeSlotsAsync(int consultantProfileId, DateTime date, TimeSpan start, TimeSpan end);
    Task<List<TimeSlot>> CreateBulkTimeSlotsAsync(int consultantProfileId, DateOnly from, DateOnly to, TimeSpan start, TimeSpan end);
    Task<bool> RemoveTimeSlotAsync(int slotId, int consultantProfileId);
    Task<List<Appointment>> GetAppointmentsAsync(int consultantProfileId, AppointmentStatus? status);
    Task<bool> MarkCompletedAsync(int appointmentId, int consultantProfileId);
}
