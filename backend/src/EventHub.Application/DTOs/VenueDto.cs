namespace EventHub.Application.DTOs;

public record VenueDto(Guid Id, string Name, string Address, int MaxCapacity);
