using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Extensions;
using EventHub.Application.DTOs;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.IntegrationTests.Bookings;

[Collection(IntegrationCollection.Name)]
public class BookingTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateBooking_WhenEventIsFull_ReturnsConflict()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, EventStatus.Published, capacity: 1);
        var firstAttendeeId = await Factory.SeedAttendeeAsync("Attendee One");
        var secondAttendeeId = await Factory.SeedAttendeeAsync("Attendee Two");

        var firstBooking = new CreateBookingDto(@event.Id, firstAttendeeId, Guid.NewGuid());
        var firstResponse = await Client.PostAsJsonAsync("/api/bookings", firstBooking, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondBooking = new CreateBookingDto(@event.Id, secondAttendeeId, Guid.NewGuid());
        var secondResponse = await Client.PostAsJsonAsync("/api/bookings", secondBooking, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var error = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("Booking.CannotAcceptBooking", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_SameAttendeeTwiceForSameEvent_ReturnsConflictWithAlreadyBookedCode()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, EventStatus.Published, capacity: 10);
        var attendeeId = await Factory.SeedAttendeeAsync();

        var firstBooking = new CreateBookingDto(@event.Id, attendeeId, Guid.NewGuid());
        var firstResponse = await Client.PostAsJsonAsync("/api/bookings", firstBooking, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // A different IdempotencyKey: a genuinely separate booking attempt, not an idempotent replay.
        var secondBooking = new CreateBookingDto(@event.Id, attendeeId, Guid.NewGuid());
        var secondResponse = await Client.PostAsJsonAsync("/api/bookings", secondBooking, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var error = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("Booking.AlreadyBooked", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_WithRepeatedIdempotencyKey_ReturnsOriginalBookingWithoutDuplicating()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, EventStatus.Published, capacity: 10);
        var attendeeId = await Factory.SeedAttendeeAsync();
        var idempotencyKey = Guid.NewGuid();
        var dto = new CreateBookingDto(@event.Id, attendeeId, idempotencyKey);

        var firstResponse = await Client.PostAsJsonAsync("/api/bookings", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstBooking = (await firstResponse.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;

        // Same request, resubmitted (e.g. client retried after a timeout) — same IdempotencyKey.
        var secondResponse = await Client.PostAsJsonAsync("/api/bookings", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var secondBooking = (await secondResponse.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;

        Assert.Equal(firstBooking.Id, secondBooking.Id);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        var rowCount = await context.Bookings.CountAsync(b => b.IdempotencyKey == idempotencyKey);
        Assert.Equal(1, rowCount);
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.Cancelled)]
    public async Task CreateBooking_ForEventNotOpenForBooking_ReturnsConflict(EventStatus status)
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, status, capacity: 10);
        var attendeeId = await Factory.SeedAttendeeAsync();

        var dto = new CreateBookingDto(@event.Id, attendeeId, Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/bookings", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_ForNonexistentEvent_ReturnsUnprocessableEntity()
    {
        var attendeeId = await Factory.SeedAttendeeAsync();

        var dto = new CreateBookingDto(Guid.NewGuid(), attendeeId, Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/bookings", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("Booking.EventNotFound", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_ForNonexistentAttendee_ReturnsUnprocessableEntity()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, EventStatus.Published, capacity: 10);

        var dto = new CreateBookingDto(@event.Id, Guid.NewGuid(), Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/bookings", dto, JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("Booking.AttendeeNotFound", error!.Code);
    }
}
