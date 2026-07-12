namespace EventHub.Application.DTOs;

public record CreateBookingDto(Guid EventId, Guid AttendeeId, Guid IdempotencyKey);
