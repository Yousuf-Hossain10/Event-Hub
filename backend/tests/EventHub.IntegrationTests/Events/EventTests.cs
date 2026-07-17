using System.Net.Http.Json;
using EventHub.Application.DTOs;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.IntegrationTests.Events;

[Collection(IntegrationCollection.Name)]
public class EventTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetEventById_And_ListEvents_ReportConfirmedBookingCount_ExcludingCancelled()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, EventStatus.Published, capacity: 10);
        var firstAttendeeId = await Factory.SeedAttendeeAsync("Attendee One");
        var secondAttendeeId = await Factory.SeedAttendeeAsync("Attendee Two");

        var beforeAnyBookings = await Client.GetFromJsonAsync<EventDto>($"/api/events/{@event.Id}", JsonOptions);
        Assert.Equal(0, beforeAnyBookings!.ConfirmedBookingCount);

        var bookingDto = new CreateBookingDto(@event.Id, firstAttendeeId, Guid.NewGuid());
        var bookingResponse = await Client.PostAsJsonAsync("/api/bookings", bookingDto, JsonOptions);
        bookingResponse.EnsureSuccessStatusCode();

        var afterOneConfirmed = await Client.GetFromJsonAsync<EventDto>($"/api/events/{@event.Id}", JsonOptions);
        Assert.Equal(1, afterOneConfirmed!.ConfirmedBookingCount);

        // Seeded directly — no Booking cancel/update endpoint exists yet. A cancelled booking must
        // not count toward capacity.
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
            context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                EventId = @event.Id,
                AttendeeId = secondAttendeeId,
                Status = BookingStatus.Cancelled,
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = Guid.NewGuid(),
            });
            await context.SaveChangesAsync();
        }

        var afterCancelledBooking = await Client.GetFromJsonAsync<EventDto>($"/api/events/{@event.Id}", JsonOptions);
        Assert.Equal(1, afterCancelledBooking!.ConfirmedBookingCount);

        // List endpoint must report the same count — exercises the batch-count path in
        // EventService.GetAllAsync, not just the single-event GetByIdAsync path.
        var list = await Client.GetFromJsonAsync<List<EventDto>>("/api/events", JsonOptions);
        var listed = list!.Single(e => e.Id == @event.Id);
        Assert.Equal(1, listed.ConfirmedBookingCount);
    }
}
