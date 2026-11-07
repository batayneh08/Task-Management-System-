using System.ComponentModel.DataAnnotations;
using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.Entities;
using ManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;

namespace ManagementSystem.Infrastructure.Data;

public sealed class DatabaseSeeder
{
    private readonly TaskManagementDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        TaskManagementDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = await GetOrCreateUserAsync("BootstrapAdmin", UserRole.Admin, cancellationToken);
        if (admin is null)
        {
            _logger.LogInformation("Bootstrap admin is not configured; development seed data was skipped.");
            return;
        }

        var demoUser = await GetOrCreateUserAsync("BootstrapDemoUser", UserRole.User, cancellationToken);
        if (!await _dbContext.Tasks.AnyAsync(cancellationToken))
        {
            var assignee = demoUser ?? admin;
            var now = DateTimeOffset.UtcNow;

            _dbContext.Tasks.AddRange(
                CreateSampleTask(admin, assignee, "Review project requirements", TaskStatus.InProgress, TaskPriority.High, now.AddDays(2)),
                CreateSampleTask(admin, assignee, "Prepare API documentation", TaskStatus.ToDo, TaskPriority.Medium, now.AddDays(5)),
                CreateSampleTask(admin, assignee, "Confirm initial database schema", TaskStatus.Completed, TaskPriority.Low, now.AddDays(-1)));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<User?> GetOrCreateUserAsync(
        string sectionName,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection(sectionName);
        var fullName = section["FullName"];
        var email = section["Email"];
        var password = section["Password"];
        var hasAnyValue = !string.IsNullOrWhiteSpace(fullName) ||
                          !string.IsNullOrWhiteSpace(email) ||
                          !string.IsNullOrWhiteSpace(password);

        if (!hasAnyValue)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2 ||
            string.IsNullOrWhiteSpace(email) || email.Trim().Length > 254 ||
            !new EmailAddressAttribute().IsValid(email.Trim()) ||
            string.IsNullOrWhiteSpace(password) || password.Length is < 8 or > 128)
        {
            throw new InvalidOperationException(
                $"Configuration section '{sectionName}' must include a valid FullName, Email, and Password (8-128 characters).");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.Role != role)
            {
                throw new InvalidOperationException(
                    $"Configured seed account '{normalizedEmail}' already exists with a different role; it was not changed.");
            }

            return existingUser;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            Role = role
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        await _dbContext.Users.AddAsync(user, cancellationToken);
        return user;
    }

    private static TaskItem CreateSampleTask(
        User creator,
        User assignee,
        string title,
        TaskStatus status,
        TaskPriority priority,
        DateTimeOffset dueDate)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Development sample data; safe to remove after local verification.",
            Status = status,
            Priority = priority,
            DueDate = dueDate,
            CreatedById = creator.Id,
            CreatedBy = creator,
            AssignedToId = assignee.Id,
            AssignedTo = assignee
        };

        task.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            Task = task,
            TaskId = task.Id,
            TaskTitle = task.Title,
            User = creator,
            UserId = creator.Id,
            Action = "Created",
            NewValue = "Sample task created during development seed."
        });

        return task;
    }
}
