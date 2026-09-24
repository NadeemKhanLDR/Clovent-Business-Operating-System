using Clovent.Restaurant.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="EmployeeAttendanceSession"/> aggregate.</summary>
internal sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<EmployeeAttendanceSession>
{
    public void Configure(EntityTypeBuilder<EmployeeAttendanceSession> builder)
    {
        builder.ToTable("AttendanceSessions", "Restaurant");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.AttendanceSessionIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.UserId)
            .HasConversion(ValueConverters.UserIdConverter)
            .IsRequired();
        builder.HasIndex(s => s.UserId);

        // Concurrency / Domain invariant: At most ONE open attendance session per employee!
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_AttendanceSessions_UserId_Open")
            .HasFilter("[PunchOutAtUtc] IS NULL")
            .IsUnique();

        builder.Property(s => s.UserName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(s => s.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();
        builder.HasIndex(s => s.BranchId);

        builder.Property(s => s.BranchName)
            .HasMaxLength(150);

        builder.Property(s => s.PunchInTerminalId)
            .HasConversion(ValueConverters.NullableTerminalIdConverter);

        builder.Property(s => s.PunchOutTerminalId)
            .HasConversion(ValueConverters.NullableTerminalIdConverter);

        builder.Property(s => s.PunchInAtUtc)
            .IsRequired();
        builder.HasIndex(s => s.PunchInAtUtc);

        builder.Property(s => s.PunchOutAtUtc);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(s => s.Status);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired();
    }
}
