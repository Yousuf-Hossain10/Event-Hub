using EventHub.Domain.Common;

namespace EventHub.Application.Common;

public static class EventErrors
{
    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "Event.ConcurrencyConflict",
        "The event was modified by another request. Reload and try again.");

    public static Error NotFound(Guid id) => Error.NotFound("Event.NotFound", $"Event '{id}' was not found.");

    public static Error VenueNotFound(Guid venueId) => Error.Unprocessable(
        "Event.VenueNotFound",
        $"Venue '{venueId}' does not exist.");
}
