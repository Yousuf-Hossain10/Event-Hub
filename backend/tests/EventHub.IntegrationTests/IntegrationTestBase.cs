using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventHub.Application.DTOs;
using EventHub.Domain.Enums;

namespace EventHub.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }

    protected CustomWebApplicationFactory Factory { get; }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<VenueDto> CreateVenueAsync(string name = "Test Venue", int maxCapacity = 100)
    {
        var dto = new CreateVenueDto(name, "1 Test St", maxCapacity);
        var response = await Client.PostAsJsonAsync("/api/venues", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VenueDto>(JsonOptions))!;
    }

    protected async Task<EventDto> CreateEventAsync(
        Guid venueId,
        EventStatus status = EventStatus.Published,
        int capacity = 50,
        DateTime? startDate = null)
    {
        var dto = new CreateEventDto(
            "Test Event",
            "Description",
            startDate ?? DateTime.UtcNow.AddDays(30),
            capacity,
            status,
            venueId);
        var response = await Client.PostAsJsonAsync("/api/events", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>(JsonOptions))!;
    }
}
