using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int Capacity { get; set; }
    public EventStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public bool IsDeleted { get; set; }

    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
