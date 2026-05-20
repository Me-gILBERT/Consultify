using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Consultify.Web.Tests.Services;

public class ConsultantServiceTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateTimeSlotsAsync_Creates30MinSlots()
    {
        var context = CreateContext(nameof(CreateTimeSlotsAsync_Creates30MinSlots));

        var profile = new ConsultantProfile { UserId = Guid.NewGuid() };
        context.ConsultantProfiles.Add(profile);
        await context.SaveChangesAsync();

        var service = new ConsultantService(context);
        var date = DateTime.UtcNow.Date.AddDays(1);

        var slots = await service.CreateTimeSlotsAsync(profile.Id, date, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0));

        Assert.Equal(6, slots.Count);
        Assert.All(slots, s => Assert.Equal(30, (s.EndTime - s.StartTime).TotalMinutes));
        Assert.All(slots, s => Assert.False(s.IsBooked));
    }

    [Fact]
    public async Task CreateTimeSlotsAsync_ThrowsForPastDate()
    {
        var context = CreateContext(nameof(CreateTimeSlotsAsync_ThrowsForPastDate));

        var profile = new ConsultantProfile { UserId = Guid.NewGuid() };
        context.ConsultantProfiles.Add(profile);
        await context.SaveChangesAsync();

        var service = new ConsultantService(context);
        var pastDate = DateTime.UtcNow.Date.AddDays(-1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTimeSlotsAsync(profile.Id, pastDate, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0)));
    }

    [Fact]
    public async Task RemoveTimeSlotAsync_OnlyRemovesUnbooked()
    {
        var context = CreateContext(nameof(RemoveTimeSlotAsync_OnlyRemovesUnbooked));

        var profile = new ConsultantProfile { UserId = Guid.NewGuid() };
        context.ConsultantProfiles.Add(profile);

        var bookedSlot = new TimeSlot
        {
            ConsultantProfileId = profile.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            IsBooked = true
        };
        var freeSlot = new TimeSlot
        {
            ConsultantProfileId = profile.Id,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            IsBooked = false
        };
        context.TimeSlots.AddRange(bookedSlot, freeSlot);
        await context.SaveChangesAsync();

        var service = new ConsultantService(context);

        var result1 = await service.RemoveTimeSlotAsync(bookedSlot.Id, profile.Id);
        Assert.False(result1);

        var result2 = await service.RemoveTimeSlotAsync(freeSlot.Id, profile.Id);
        Assert.True(result2);
    }
}
