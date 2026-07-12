using EventHub.Application.DTOs;
using EventHub.Application.Services;

namespace EventHub.Api.GraphQL;

public class Query
{
    [UsePaging]
    public IQueryable<EventDto> GetEvents([Service] IEventService eventService) =>
        eventService.GetEventsQueryable();

    public async Task<EventDto> GetEventById(
        Guid id,
        [Service] IEventService eventService,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            throw new GraphQLException(result.Error.Message);
        }

        return result.Value;
    }

    public async Task<IEnumerable<VenueDto>> GetVenues(
        [Service] IVenueService venueService,
        CancellationToken cancellationToken) =>
        await venueService.GetAllAsync(cancellationToken);

    public async Task<VenueDto> GetVenueById(
        Guid id,
        [Service] IVenueService venueService,
        CancellationToken cancellationToken)
    {
        var result = await venueService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            throw new GraphQLException(result.Error.Message);
        }

        return result.Value;
    }
}
