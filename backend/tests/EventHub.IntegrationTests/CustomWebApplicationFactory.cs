using EventHub.Domain.Entities;
using EventHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabaseName = "EventHubDb_IntegrationTests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var baseConnectionString = configBuilder.Build().GetConnectionString("EventHubDb")
                ?? throw new InvalidOperationException("EventHubDb connection string is not configured.");

            var testConnectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = TestDatabaseName
            }.ConnectionString;

            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EventHubDb"] = testConnectionString
            });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();

        // Delete in FK-dependency order; IgnoreQueryFilters picks up rows soft-deleted by prior tests.
        context.Bookings.RemoveRange(context.Bookings.IgnoreQueryFilters());
        context.Events.RemoveRange(context.Events.IgnoreQueryFilters());
        context.Venues.RemoveRange(context.Venues.IgnoreQueryFilters());
        context.Attendees.RemoveRange(context.Attendees.IgnoreQueryFilters());

        await context.SaveChangesAsync();
    }

    // No REST endpoint creates Attendees yet (out of scope for this step); tests seed directly.
    public async Task<Guid> SeedAttendeeAsync(string name = "Test Attendee")
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();

        var attendee = new Attendee
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = $"{Guid.NewGuid()}@test.local"
        };

        context.Attendees.Add(attendee);
        await context.SaveChangesAsync();

        return attendee.Id;
    }
}
