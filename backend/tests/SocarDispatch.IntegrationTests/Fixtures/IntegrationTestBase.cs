using Microsoft.Extensions.DependencyInjection;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Infrastructure.Persistence;
using SocarDispatch.IntegrationTests.Fixtures;

namespace SocarDispatch.IntegrationTests.Common;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected HttpClient Client = null!;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    public virtual async Task InitializeAsync()
    {
        // Reset database state using Respawn before each test run
        await Factory.ResetDatabaseAsync();
        Client = Factory.CreateClient();
    }

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }

    protected async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(context);
    }

    protected async Task<User> SeedUserAsync(
        string firstName = "Ali",
        string lastName = "Demir",
        string? email = null,
        RoleType role = RoleType.Team,
        string department = "Refinery Operations",
        string? phone = null)
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = email ?? $"user_{Guid.NewGuid():N}@socar.az",
                Phone = phone ?? $"+90555{Random.Shared.Next(1000000, 9999999)}",
                PasswordHash = "hash123",
                Department = department,
                RoleType = role,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        });
    }


    protected async Task<Team> SeedTeamAsync(
        string teamName = "Star Response Unit",
        TeamStatus status = TeamStatus.Idle,
        Guid? leaderId = null)
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                TeamName = teamName,
                Status = status,
                LeaderId = leaderId,
                CurrentLatitude = 38.7900m,
                CurrentLongitude = 26.9200m,
                UpdatedAt = DateTime.UtcNow
            };
            context.Teams.Add(team);
            await context.SaveChangesAsync();
            return team;
        });
    }
}
