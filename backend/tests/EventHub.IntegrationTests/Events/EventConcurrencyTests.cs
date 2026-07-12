using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventHub.Application.DTOs;
using EventHub.Domain.Enums;

namespace EventHub.IntegrationTests.Events;

public class EventConcurrencyTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdateEvent_ReusingStaleRowVersion_ReturnsConflict()
    {
        var venue = await CreateVenueAsync();
        var @event = await CreateEventAsync(venue.Id);

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
        var firstResponse = await _client.PutAsJsonAsync($"/api/events/{@event.Id}", firstEditDto, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Second update reuses the same (now stale) RowVersion the first attempt started from.
        var secondResponse = await _client.PutAsJsonAsync($"/api/events/{@event.Id}", secondEditDto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    private async Task<VenueDto> CreateVenueAsync()
    {
        var dto = new CreateVenueDto("Concurrency Test Venue", "1 Test St", 100);
        var response = await _client.PostAsJsonAsync("/api/venues", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VenueDto>(JsonOptions))!;
    }

    private async Task<EventDto> CreateEventAsync(Guid venueId)
    {
        var dto = new CreateEventDto(
            "Concurrency Test Event",
            "Description",
            DateTime.UtcNow.AddDays(30),
            50,
            EventStatus.Draft,
            venueId);
        var response = await _client.PostAsJsonAsync("/api/events", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>(JsonOptions))!;
    }
}
