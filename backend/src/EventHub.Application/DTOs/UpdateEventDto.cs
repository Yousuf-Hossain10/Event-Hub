using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs;

public record UpdateEventDto(
    string Title,
    string Description,
    DateTime StartDate,
    int Capacity,
    EventStatus Status,
    Guid VenueId);
