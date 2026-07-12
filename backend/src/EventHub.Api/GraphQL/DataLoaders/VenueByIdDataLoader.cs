using EventHub.Application.DTOs;
using EventHub.Application.Services;
using GreenDonut;

namespace EventHub.Api.GraphQL.DataLoaders;

public class VenueByIdDataLoader(
    IVenueService venueService,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, VenueDto>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, VenueDto>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        var venues = await venueService.GetByIdsAsync(keys, cancellationToken);
        return venues.ToDictionary(v => v.Id);
    }
}
