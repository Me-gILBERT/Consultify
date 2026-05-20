using Moq;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Consultify.Web.Tests.Services;

public class BookingServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAvailableSlots_ReturnsOnlyUnbookedFutureSlots()
    {
        var context = CreateContext(nameof(GetAvailableSlots_ReturnsOnlyUnbookedFutureSlots));

        var profile = new ConsultantProfile { UserId = Guid.NewGuid() };
        context.ConsultantProfiles.Add(profile);

        context.TimeSlots.AddRange(
            new TimeSlot { ConsultantProfileId = profile.Id, StartTime = DateTime.UtcNow.AddDays(1), EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30), IsBooked = false },
            new TimeSlot { ConsultantProfileId = profile.Id, StartTime = DateTime.UtcNow.AddDays(-1), EndTime = DateTime.UtcNow.AddDays(-1).AddMinutes(30), IsBooked = false },
            new TimeSlot { ConsultantProfileId = profile.Id, StartTime = DateTime.UtcNow.AddDays(2), EndTime = DateTime.UtcNow.AddDays(2).AddMinutes(30), IsBooked = true }
        );
        await context.SaveChangesAsync();

        var service = new BookingService(context);
        var result = await service.GetAvailableSlotsAsync(profile.Id);

        Assert.Single(result);
        Assert.False(result[0].IsBooked);
        Assert.True(result[0].StartTime > DateTime.UtcNow);
    }

    [Fact]
    public async Task BookSlotAsync_ThrowsWhenSlotAlreadyBooked()
    {
        var context = CreateContext(nameof(BookSlotAsync_ThrowsWhenSlotAlreadyBooked));

        var profile = new ConsultantProfile { UserId = Guid.NewGuid() };
        context.ConsultantProfiles.Add(profile);

        var slot = new TimeSlot
        {
            ConsultantProfileId = profile.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            IsBooked = true
        };
        context.TimeSlots.Add(slot);
        await context.SaveChangesAsync();

        var service = new BookingService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BookSlotAsync(slot.Id, Guid.NewGuid(), null));
    }

    [Fact]
    public void CanCancel_ReturnsFalseForPastAppointment()
    {
        var appointment = new Appointment
        {
            Status = AppointmentStatus.Scheduled,
            TimeSlot = new TimeSlot
            {
                StartTime = DateTime.UtcNow.AddHours(1)
            }
        };

        var service = new BookingService(null!);
        var result = service.CanCancel(appointment);

        Assert.False(result);
    }
}
