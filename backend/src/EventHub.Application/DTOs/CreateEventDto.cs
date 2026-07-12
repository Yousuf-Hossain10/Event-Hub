using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs;

public record CreateEventDto(
    string Title,
    string Description,
    DateTime StartDate,
    int Capacity,
    EventStatus Status,
    Guid VenueId);
