using EventHub.Application.DTOs;
using EventHub.Domain.Common;

namespace EventHub.Application.Services;

public interface IVenueService
{
    Task<IReadOnlyList<VenueDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<VenueDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VenueDto> CreateAsync(CreateVenueDto dto, CancellationToken cancellationToken = default);
    Task<Result<VenueDto>> UpdateAsync(Guid id, UpdateVenueDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
