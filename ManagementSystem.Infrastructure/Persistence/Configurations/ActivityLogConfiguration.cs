using ManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.TaskTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(activity => activity.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(activity => activity.OldValue)
            .HasMaxLength(4000);

        builder.Property(activity => activity.NewValue)
            .HasMaxLength(4000);

        builder.Property(activity => activity.CreatedAt)
            .IsRequired();

        builder.HasOne(activity => activity.Task)
            .WithMany(task => task.ActivityLogs)
            .HasForeignKey(activity => activity.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(activity => activity.User)
            .WithMany()
            .HasForeignKey(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(activity => new { activity.TaskId, activity.CreatedAt });
        builder.HasIndex(activity => new { activity.UserId, activity.CreatedAt });
    }
}
