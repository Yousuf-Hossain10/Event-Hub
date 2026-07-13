using EventHub.Domain.Common;

namespace EventHub.Application.Common;

public static class BookingErrors
{
    public static Error EventNotFound(Guid eventId) => Error.Unprocessable(
        "Booking.EventNotFound",
        $"Event '{eventId}' does not exist.");

    public static Error AttendeeNotFound(Guid attendeeId) => Error.Unprocessable(
        "Booking.AttendeeNotFound",
        $"Attendee '{attendeeId}' does not exist.");

    public static Error CannotAcceptBooking(Guid eventId) => Error.Conflict(
        "Booking.CannotAcceptBooking",
        $"Event '{eventId}' cannot accept new bookings right now.");

    public static Error AlreadyBooked(Guid eventId, Guid attendeeId) => Error.Conflict(
        "Booking.AlreadyBooked",
        $"Attendee '{attendeeId}' already has a confirmed booking for event '{eventId}'.");
}
