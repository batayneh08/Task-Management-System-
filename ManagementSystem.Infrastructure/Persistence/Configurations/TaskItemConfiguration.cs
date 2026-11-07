using ManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasMaxLength(4000);

        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(task => task.DueDate)
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .IsRequired();

        builder.Property(task => task.UpdatedAt)
            .IsRequired();

        builder.HasOne(task => task.CreatedBy)
            .WithMany(user => user.CreatedTasks)
            .HasForeignKey(task => task.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.AssignedTo)
            .WithMany(user => user.AssignedTasks)
            .HasForeignKey(task => task.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(task => task.Title);
        builder.HasIndex(task => task.Status);
        builder.HasIndex(task => task.Priority);
        builder.HasIndex(task => task.AssignedToId);
        builder.HasIndex(task => task.DueDate);
    }
}
