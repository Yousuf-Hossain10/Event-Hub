namespace EventHub.Application.Common;

public enum BookingCreationOutcome
{
    Created,

    // Lost a race against another request creating a Booking with the same IdempotencyKey; the
    // caller should look up and return the winner's booking instead of surfacing an error.
    DuplicateIdempotencyKey
}
