using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ManagementSystem.Application.Interfaces;
using ManagementSystem.Domain.DTOs;
using ManagementSystem.Domain.Entities;
using ManagementSystem.Domain.Enums;
using ManagementSystem.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using TaskStatus = ManagementSystem.Domain.Enums.TaskStatus;
using Xunit;

namespace ManagementSystem.Tests;

public sealed class BackendIntegrationTests
{
    [Fact]
    public async Task AnonymousRequestsCannotReadTasks()
    {
        using var factory = new TaskManagementApiFactory();
        await factory.ResetDatabaseAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tasks", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidAndDuplicateRegistrationsReturnExpectedErrors()
    {
        using var factory = new TaskManagementApiFactory();
        await factory.ResetDatabaseAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        var invalidResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            FullName = "A",
            Email = "not-an-email",
            Password = "short"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        var registration = new RegisterRequestDto
        {
            FullName = "Unique User",
            Email = $"user-{Guid.NewGuid():N}@example.test",
            Password = "ValidPass123"
        };
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/auth/register", registration, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/auth/register", registration, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Email = registration.Email,
                Password = "WrongPass123"
            }, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task AdminManagesTaskDetailsWhileAssigneeCanUpdateStatusAndProgress()
    {
        using var factory = new TaskManagementApiFactory();
        await factory.ResetDatabaseAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        var regularUserResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            FullName = "Integration User",
            Email = $"user-{Guid.NewGuid():N}@example.test",
            Password = "ValidPass123"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, regularUserResponse.StatusCode);
        var registeredUser = await regularUserResponse.Content.ReadFromJsonAsync<AuthResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(registeredUser);
        Assert.Equal(UserRole.User, registeredUser.User.Role);

        var anotherUserResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            FullName = "Another User",
            Email = $"another-{Guid.NewGuid():N}@example.test",
            Password = "ValidPass123"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, anotherUserResponse.StatusCode);
        var anotherUser = await anotherUserResponse.Content.ReadFromJsonAsync<AuthResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(anotherUser);

        string adminToken;
        await using (var adminCreationScope = factory.Services.CreateAsyncScope())
        {
            var adminDbContext = adminCreationScope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var admin = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Integration Admin",
                Email = $"admin-{Guid.NewGuid():N}@example.test",
                PasswordHash = "A test-only hash string",
                Role = UserRole.Admin
            };
            adminDbContext.Users.Add(admin);
            await adminDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            adminToken = adminCreationScope.ServiceProvider.GetRequiredService<ITokenGenerator>().GenerateToken(admin).Token;
        }

        var createTask = new CreateTaskRequestDto
        {
            Title = "Integration task",
            Description = "Created through the API",
            Priority = TaskPriority.High,
            DueDate = DateTimeOffset.UtcNow.AddDays(2),
            AssignedToId = registeredUser.User.Id
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registeredUser.AccessToken);
        var forbiddenCreateResponse = await client.PostAsJsonAsync(
            "/api/tasks", createTask, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreateResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var createResponse = await client.PostAsJsonAsync(
            "/api/tasks", createTask, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(createdTask);
        Assert.NotEqual(registeredUser.User.Id, createdTask.CreatedBy.Id);
        Assert.Equal(registeredUser.User.Id, createdTask.AssignedTo.Id);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", anotherUser.AccessToken);
        var unrelatedUserStatusResponse = await client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new UpdateTaskRequestDto
        {
            Title = createdTask.Title,
            Description = createdTask.Description,
            Status = TaskStatus.InProgress,
            Priority = createdTask.Priority,
            DueDate = createdTask.DueDate,
            AssignedToId = createdTask.AssignedTo.Id
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedUserStatusResponse.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registeredUser.AccessToken);
        var progressNote = "Implemented input validation and fixed the login flow.";
        var updateResponse = await client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new UpdateTaskRequestDto
        {
            Title = createdTask.Title,
            Description = createdTask.Description,
            Status = TaskStatus.InProgress,
            Priority = createdTask.Priority,
            DueDate = createdTask.DueDate,
            AssignedToId = createdTask.AssignedTo.Id,
            ProgressSummary = progressNote
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedTask = await updateResponse.Content.ReadFromJsonAsync<TaskResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTask);
        Assert.Equal(TaskStatus.InProgress, updatedTask.Status);

        var forbiddenDescriptionResponse = await client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new UpdateTaskRequestDto
        {
            Title = updatedTask.Title,
            Description = "Changed by the assignee",
            Status = updatedTask.Status,
            Priority = updatedTask.Priority,
            DueDate = updatedTask.DueDate,
            AssignedToId = updatedTask.AssignedTo.Id
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDescriptionResponse.StatusCode);

        var forbiddenPriorityResponse = await client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new UpdateTaskRequestDto
        {
            Title = updatedTask.Title,
            Description = updatedTask.Description,
            Status = updatedTask.Status,
            Priority = TaskPriority.Low,
            DueDate = updatedTask.DueDate,
            AssignedToId = updatedTask.AssignedTo.Id
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPriorityResponse.StatusCode);

        var forbiddenDueDateResponse = await client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new UpdateTaskRequestDto
        {
            Title = updatedTask.Title,
            Description = updatedTask.Description,
            Status = updatedTask.Status,
            Priority = updatedTask.Priority,
            DueDate = DateTimeOffset.UtcNow.AddDays(10),
            AssignedToId = updatedTask.AssignedTo.Id
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDueDateResponse.StatusCode);

        var activity = await client.GetFromJsonAsync<IReadOnlyList<ActivityLogResponseDto>>(
            $"/api/tasks/{createdTask.Id}/activity", TestContext.Current.CancellationToken);
        Assert.NotNull(activity);
        Assert.Contains(activity, item => item.Action == "Created");
        var userUpdate = Assert.Single(activity, item => item.Action == "Updated Status" && item.UserId == registeredUser.User.Id);
        Assert.Contains(progressNote, userUpdate.NewValue);

        var forbiddenReassignmentResponse = await client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new UpdateTaskRequestDto
        {
            Title = updatedTask.Title,
            Description = updatedTask.Description,
            Status = updatedTask.Status,
            Priority = updatedTask.Priority,
            DueDate = updatedTask.DueDate,
            AssignedToId = anotherUser.User.Id
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenReassignmentResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync(
            $"/api/tasks/{createdTask.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminReassignmentResponse = await client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", new UpdateTaskRequestDto
        {
            Title = updatedTask.Title,
            Description = updatedTask.Description,
            Status = updatedTask.Status,
            Priority = updatedTask.Priority,
            DueDate = updatedTask.DueDate,
            AssignedToId = anotherUser.User.Id
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, adminReassignmentResponse.StatusCode);
        var reassignedTask = await adminReassignmentResponse.Content.ReadFromJsonAsync<TaskResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(reassignedTask);
        Assert.Equal(anotherUser.User.Id, reassignedTask.AssignedTo.Id);

        var adminActivity = await client.GetFromJsonAsync<IReadOnlyList<ActivityLogResponseDto>>(
            $"/api/tasks/{createdTask.Id}/activity", TestContext.Current.CancellationToken);
        Assert.NotNull(adminActivity);
        var statusActivity = Assert.Single(adminActivity, item => item.Action == "Updated Status" && item.UserId == registeredUser.User.Id);
        Assert.Equal(TaskStatus.ToDo.ToString(), statusActivity.OldValue);
        using (var statusDetails = JsonDocument.Parse(statusActivity.NewValue!))
        {
            Assert.Equal(TaskStatus.InProgress.ToString(), statusDetails.RootElement.GetProperty("Value").GetString());
            Assert.Equal(progressNote, statusDetails.RootElement.GetProperty("Summary").GetString());
        }
        Assert.Contains(adminActivity, item => item.Action == "Updated Assignee" && item.UserId == createdTask.CreatedBy.Id && item.NewValue == "Another User");

        var adminDeleteResponse = await client.DeleteAsync(
            $"/api/tasks/{createdTask.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, adminDeleteResponse.StatusCode);

        var getDeletedResponse = await client.GetAsync(
            $"/api/tasks/{createdTask.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);

        await using var deleteLogScope = factory.Services.CreateAsyncScope();
        var deleteLogDbContext = deleteLogScope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
        var preservedDeleteActivity = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .SingleAsync(deleteLogDbContext.ActivityLogs, log => log.Action == "Deleted", TestContext.Current.CancellationToken);
        Assert.NotNull(preservedDeleteActivity);
        Assert.Equal("Deleted", preservedDeleteActivity.Action);
        Assert.Null(preservedDeleteActivity.TaskId);
    }

    [Fact]
    public async Task AuthenticatedUserCanReadAssigneeList()
    {
        using var factory = new TaskManagementApiFactory();
        await factory.ResetDatabaseAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            FullName = "Regular User",
            Email = $"user-{Guid.NewGuid():N}@example.test",
            Password = "ValidPass123"
        }, TestContext.Current.CancellationToken);
        var user = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(user);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
        var response = await client.GetAsync("/api/users", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserResponseDto>>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(users);
        Assert.Single(users);
    }

    [Fact]
    public async Task AdminCanReadUserManagementEndpoint()
    {
        using var factory = new TaskManagementApiFactory();
        await factory.ResetDatabaseAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        string adminToken;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var admin = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Test Admin",
                Email = $"admin-{Guid.NewGuid():N}@example.test",
                PasswordHash = "A test-only hash string",
                Role = UserRole.Admin
            };
            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var tokenGenerator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();
            adminToken = tokenGenerator.GenerateToken(admin).Token;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await client.GetAsync("/api/users", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserResponseDto>>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(users);
        var returnedAdmin = Assert.Single(users);
        Assert.Equal(UserRole.Admin, returnedAdmin.Role);
    }

    [Fact]
    public async Task SwaggerUiDocumentsTheApiAndBearerAuthentication()
    {
        using var factory = new TaskManagementApiFactory();
        await factory.ResetDatabaseAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        var uiResponse = await client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);
        var specificationResponse = await client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, specificationResponse.StatusCode);

        var specification = await specificationResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("/api/auth/login", specification);
        Assert.Contains("/api/tasks", specification);
        Assert.Contains("Bearer", specification);
    }

}
