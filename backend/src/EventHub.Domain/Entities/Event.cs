using EventHub.Domain.Common;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int Capacity { get; set; }
    public EventStatus Status { get; private set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public bool IsDeleted { get; set; }

    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public static Event Create(
        Guid id,
        string title,
        string description,
        DateTime startDate,
        int capacity,
        EventStatus status,
        Guid venueId) => new()
        {
            Id = id,
            Title = title,
            Description = description,
            StartDate = startDate,
            Capacity = capacity,
            Status = status,
            VenueId = venueId
        };

    public Result ChangeStatus(EventStatus newStatus)
    {
        if (newStatus == Status)
        {
            return Result.Success();
        }

        var isValidTransition = (Status, newStatus) switch
        {
            (EventStatus.Draft, EventStatus.Published) => true,
            (EventStatus.Draft, EventStatus.Cancelled) => true,
            (EventStatus.Published, EventStatus.Cancelled) => true,
            (EventStatus.Published, EventStatus.Completed) => true,
            _ => false
        };

        if (!isValidTransition)
        {
            return Result.Failure(Errors.InvalidStatusTransition(Status, newStatus));
        }

        Status = newStatus;
        return Result.Success();
    }

    // Consumed by the booking mutation in step 6 to decide whether a new Booking may be created.
    public bool CanAcceptBooking(int currentBookingCount) =>
        Status == EventStatus.Published
        && StartDate > DateTime.UtcNow
        && currentBookingCount < Capacity;

    public static class Errors
    {
        public static Error InvalidStatusTransition(EventStatus from, EventStatus to) => Error.Unprocessable(
            "Event.InvalidStatusTransition",
            $"Cannot change event status from '{from}' to '{to}'.");
    }
}
