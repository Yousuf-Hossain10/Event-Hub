using EventHub.Application.DTOs;
using EventHub.Domain.Common;

namespace EventHub.Application.Services;

public interface IBookingService
{
    Task<Result<BookingDto>> CreateAsync(CreateBookingDto dto, CancellationToken cancellationToken = default);
}
