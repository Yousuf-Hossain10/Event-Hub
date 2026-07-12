namespace EventHub.Application.Common;

// Interim signal for an invalid VenueId reference; step 4 replaces this with Result<T> + a typed failure reason.
public class VenueNotFoundException(Guid venueId) : Exception($"Venue '{venueId}' does not exist.")
{
    public Guid VenueId { get; } = venueId;
}
