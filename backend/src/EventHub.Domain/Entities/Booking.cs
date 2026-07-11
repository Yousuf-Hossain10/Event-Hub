using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid IdempotencyKey { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public bool IsDeleted { get; set; }

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid AttendeeId { get; set; }
    public Attendee Attendee { get; set; } = null!;
}
