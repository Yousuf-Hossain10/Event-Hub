namespace EventHub.Domain.Entities;

public class Venue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
