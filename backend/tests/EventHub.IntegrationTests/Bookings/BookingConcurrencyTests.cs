using System.Net;
using System.Net.Http.Json;
using EventHub.Application.DTOs;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.IntegrationTests.Bookings;

[Collection(IntegrationCollection.Name)]
public class BookingConcurrencyTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateBooking_TwoConcurrentRequestsForLastSeat_ExactlyOneSucceeds()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, EventStatus.Published, capacity: 1);
        var firstAttendeeId = await Factory.SeedAttendeeAsync("Racer One");
        var secondAttendeeId = await Factory.SeedAttendeeAsync("Racer Two");

        var firstDto = new CreateBookingDto(@event.Id, firstAttendeeId, Guid.NewGuid());
        var secondDto = new CreateBookingDto(@event.Id, secondAttendeeId, Guid.NewGuid());

        // Fire both requests without awaiting in between so they genuinely overlap in-flight; the
        // race is only exercised if both reach the DB's UPDLOCK before either commits.
        var firstTask = Client.PostAsJsonAsync("/api/bookings", firstDto, JsonOptions);
        var secondTask = Client.PostAsJsonAsync("/api/bookings", secondDto, JsonOptions);

        var responses = await Task.WhenAll(firstTask, secondTask);

        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, createdCount);
        Assert.Equal(1, conflictCount);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        var confirmedCount = await context.Bookings.CountAsync(
            b => b.EventId == @event.Id && b.Status == BookingStatus.Confirmed);
        Assert.Equal(1, confirmedCount);
    }
}
