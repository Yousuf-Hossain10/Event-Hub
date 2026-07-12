using EventHub.Api.GraphQL.DataLoaders;
using EventHub.Application.DTOs;

namespace EventHub.Api.GraphQL;

[ExtendObjectType(typeof(EventDto))]
public class EventDtoResolvers
{
    public async Task<VenueDto?> GetVenueAsync(
        [Parent] EventDto @event,
        VenueByIdDataLoader venueByIdDataLoader,
        CancellationToken cancellationToken) =>
        await venueByIdDataLoader.LoadAsync(@event.VenueId, cancellationToken);
}
