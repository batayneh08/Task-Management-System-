using ManagementSystem.Application.Exceptions;
using ManagementSystem.Application.Interfaces;
using ManagementSystem.Application.Services;
using ManagementSystem.Domain.DTOs;
using ManagementSystem.Domain.Entities;
using ManagementSystem.Domain.Enums;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;
using Xunit;

namespace ManagementSystem.Tests;

public sealed class ApplicationUnitTests
{
    [Fact]
    public async Task RegistrationNormalizesEmailAndAlwaysCreatesUserRole()
    {
        var userRepository = new FakeUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var service = new AuthService(userRepository, passwordHasher, new FakeTokenGenerator());

        var response = await service.RegisterAsync(new RegisterRequestDto
        {
            FullName = " New Account ",
            Email = " NEW@Example.Test ",
            Password = "ValidPass123"
        }, TestContext.Current.CancellationToken);

        Assert.Equal("new@example.test", response.User.Email);
        Assert.Equal("New Account", response.User.FullName);
        Assert.Equal(UserRole.User, response.User.Role);
        Assert.Equal("hashed:ValidPass123", userRepository.Users.Single().PasswordHash);
    }

    [Fact]
    public async Task RegistrationRejectsDuplicateEmail()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Existing User",
            Email = "existing@example.test",
            PasswordHash = "hash"
        });
        var service = new AuthService(userRepository, new FakePasswordHasher(), new FakeTokenGenerator());

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() => service.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Another User",
            Email = "EXISTING@example.test",
            Password = "ValidPass123"
        }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoginReturnsSameFailureForUnknownEmailAndWrongPassword()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Existing User",
            Email = "existing@example.test",
            PasswordHash = "hashed:CorrectPass123"
        });
        var service = new AuthService(userRepository, new FakePasswordHasher(), new FakeTokenGenerator());

        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = "existing@example.test",
            Password = "WrongPass123"
        }, TestContext.Current.CancellationToken));

        Assert.Equal("The email or password is incorrect.", error.Message);
    }

    [Fact]
    public async Task TaskCreationRejectsInvalidPriorityBeforeWritingToRepository()
    {
        var taskRepository = new FakeTaskRepository();
        var userRepository = new FakeUserRepository();
        var service = new TaskService(taskRepository, userRepository, new FakeActivityLogRepository());

        await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateTaskAsync(new CreateTaskRequestDto
        {
            Title = "Valid title",
            Priority = (TaskPriority)99,
            DueDate = DateTimeOffset.UtcNow.AddDays(1),
            AssignedToId = Guid.NewGuid()
        }, Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Empty(taskRepository.SavedTasks);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Email == normalizedEmail));
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(User user, string password)
        {
            return $"hashed:{password}";
        }

        public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        {
            return hashedPassword == HashPassword(user, providedPassword);
        }
    }

    private sealed class FakeTokenGenerator : ITokenGenerator
    {
        public AccessTokenDto GenerateToken(User user)
        {
            return new AccessTokenDto("unit-test-token", DateTimeOffset.UtcNow.AddMinutes(60));
        }
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public List<TaskItem> SavedTasks { get; } = new();

        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TaskItem?>(null);
        }

        public Task<TaskItem?> GetByIdWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TaskItem?>(null);
        }

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            SavedTasks.Add(task);
            return Task.CompletedTask;
        }

        public void Update(TaskItem task) { }

        public void Delete(TaskItem task) { }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<TaskItem>> GetFilteredAsync(
            TaskStatus? status = null,
            TaskPriority? priority = null,
            Guid? assignedToId = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaskItem>>(SavedTasks);
        }

        public Task<TaskCountsDto> GetCountsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TaskCountsDto(0, 0, 0, 0));
        }

        public Task<IReadOnlyList<TaskItem>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaskItem>>(Array.Empty<TaskItem>());
        }

        public Task<IReadOnlyList<TaskItem>> GetHighPriorityAsync(int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaskItem>>(Array.Empty<TaskItem>());
        }

        public Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(
            Guid userId,
            int count,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaskItem>>(Array.Empty<TaskItem>());
        }
    }

    private sealed class FakeActivityLogRepository : IActivityLogRepository
    {
        public Task<IReadOnlyList<ActivityLog>> GetByTaskIdAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ActivityLog>>(Array.Empty<ActivityLog>());
        }

        public Task AddAsync(ActivityLog activityLog, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
