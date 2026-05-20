using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Models;

namespace Consultify.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<ConsultantProfile> ConsultantProfiles => Set<ConsultantProfile>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.ConsultantProfile)
                  .WithOne(cp => cp.User)
                  .HasForeignKey<ConsultantProfile>(cp => cp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConsultantProfile>(entity =>
        {
            entity.Property(cp => cp.Bio).HasMaxLength(2000);
            entity.Property(cp => cp.Specialization).HasMaxLength(200);
            entity.Property(cp => cp.HourlyRate).HasColumnType("decimal(18,2)");
            entity.HasIndex(cp => cp.UserId).IsUnique();
        });

        builder.Entity<TimeSlot>(entity =>
        {
            entity.HasOne(ts => ts.ConsultantProfile)
                  .WithMany(cp => cp.TimeSlots)
                  .HasForeignKey(ts => ts.ConsultantProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ts => ts.Appointment)
                  .WithOne(a => a.TimeSlot)
                  .HasForeignKey<Appointment>(a => a.TimeSlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(ts => ts.ConsultantProfileId);
            entity.HasIndex(ts => ts.StartTime);
            entity.HasIndex(ts => ts.IsBooked);
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.Property(a => a.Notes).HasMaxLength(2000);
            entity.Property(a => a.CancellationReason).HasMaxLength(500);
            entity.Property(a => a.Status)
                  .HasConversion<int>();

            entity.HasOne(a => a.CustomerUser)
                  .WithMany(u => u.CustomerAppointments)
                  .HasForeignKey(a => a.CustomerUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ConsultantProfile)
                  .WithMany(cp => cp.Appointments)
                  .HasForeignKey(a => a.ConsultantProfileId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Review)
                  .WithOne(r => r.Appointment)
                  .HasForeignKey<Review>(r => r.AppointmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.CustomerUserId);
            entity.HasIndex(a => a.ConsultantProfileId);
            entity.HasIndex(a => a.Status);
        });

        builder.Entity<Review>(entity =>
        {
            entity.Property(r => r.Comment).HasMaxLength(2000);

            entity.HasOne(r => r.CustomerUser)
                  .WithMany()
                  .HasForeignKey(r => r.CustomerUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ConsultantProfile)
                  .WithMany(cp => cp.Reviews)
                  .HasForeignKey(r => r.ConsultantProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.ConsultantProfileId);
        });
    }
}
