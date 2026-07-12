using EventHub.Application.DTOs;

namespace EventHub.Application.Services;

public interface IVenueService
{
    Task<IReadOnlyList<VenueDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VenueDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VenueDto> CreateAsync(CreateVenueDto dto, CancellationToken cancellationToken = default);
    Task<VenueDto?> UpdateAsync(Guid id, UpdateVenueDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
