using EventHub.Application.Common;
using EventHub.Application.DTOs;
using EventHub.Domain.Entities;

namespace EventHub.Application.Services;

public class VenueService(IVenueRepository venueRepository) : IVenueService
{
    public async Task<IReadOnlyList<VenueDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var venues = await venueRepository.GetAllAsync(cancellationToken);
        return venues.Select(ToDto).ToList();
    }

    public async Task<VenueDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var venue = await venueRepository.GetByIdAsync(id, cancellationToken);
        return venue is null ? null : ToDto(venue);
    }

    public async Task<VenueDto> CreateAsync(CreateVenueDto dto, CancellationToken cancellationToken = default)
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Address = dto.Address,
            MaxCapacity = dto.MaxCapacity
        };

        await venueRepository.AddAsync(venue, cancellationToken);
        await venueRepository.SaveChangesAsync(cancellationToken);

        return ToDto(venue);
    }

    public async Task<VenueDto?> UpdateAsync(Guid id, UpdateVenueDto dto, CancellationToken cancellationToken = default)
    {
        var venue = await venueRepository.GetByIdAsync(id, cancellationToken);
        if (venue is null)
        {
            return null;
        }

        venue.Name = dto.Name;
        venue.Address = dto.Address;
        venue.MaxCapacity = dto.MaxCapacity;

        await venueRepository.SaveChangesAsync(cancellationToken);

        return ToDto(venue);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var venue = await venueRepository.GetByIdAsync(id, cancellationToken);
        if (venue is null)
        {
            return false;
        }

        venue.IsDeleted = true;
        await venueRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static VenueDto ToDto(Venue venue) => new(venue.Id, venue.Name, venue.Address, venue.MaxCapacity);
}
