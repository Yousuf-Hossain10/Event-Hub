using EventHub.Domain.Common;

namespace EventHub.Application.Common;

public static class VenueErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Venue.NotFound", $"Venue '{id}' was not found.");
}
