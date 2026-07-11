namespace EventHub.Domain.Entities;

public class Attendee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
