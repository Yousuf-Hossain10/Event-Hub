using System.Net;
using System.Net.Http.Json;
using EventHub.Application.DTOs;
using EventHub.Domain.Enums;

namespace EventHub.IntegrationTests.Events;

[Collection(IntegrationCollection.Name)]
public class EventConcurrencyTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UpdateEvent_ReusingStaleRowVersion_ReturnsConflict()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id, EventStatus.Draft);

        // Both attempts assert the SAME stale RowVersion captured at creation time; each carries a
        // distinct edit so EF Core actually attempts a write (a no-op change wouldn't hit the DB at all).
        var firstEditDto = new UpdateEventDto(
            "First Edit",
            @event.Description,
            @event.StartDate,
            @event.Capacity,
            @event.Status,
            @event.VenueId,
            @event.RowVersion);

        var secondEditDto = new UpdateEventDto(
            "Second Edit",
            @event.Description,
            @event.StartDate,
            @event.Capacity,
            @event.Status,
            @event.VenueId,
            @event.RowVersion);

        // First update succeeds and advances the RowVersion server-side.
        var firstResponse = await Client.PutAsJsonAsync($"/api/events/{@event.Id}", firstEditDto, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Second update reuses the same (now stale) RowVersion the first attempt started from.
        var secondResponse = await Client.PutAsJsonAsync($"/api/events/{@event.Id}", secondEditDto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }
}
