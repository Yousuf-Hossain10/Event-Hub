using EventHub.Application.Common;
using EventHub.Application.DTOs;
using EventHub.Domain.Common;
using EventHub.Domain.Entities;

namespace EventHub.Application.Services;

public class VenueService(IVenueRepository venueRepository) : IVenueService
{
    public async Task<IReadOnlyList<VenueDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var venues = await venueRepository.GetAllAsync(cancellationToken);
        return venues.Select(ToDto).ToList();
    }

    public async Task<Result<VenueDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var venue = await venueRepository.GetByIdAsync(id, cancellationToken);
        return venue is null
            ? Result.Failure<VenueDto>(VenueErrors.NotFound(id))
            : Result.Success(ToDto(venue));
    }

    public async Task<IReadOnlyList<VenueDto>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        var venues = await venueRepository.GetByIdsAsync(ids, cancellationToken);
        return venues.Select(ToDto).ToList();
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

    public async Task<Result<VenueDto>> UpdateAsync(Guid id, UpdateVenueDto dto, CancellationToken cancellationToken = default)
    {
        var venue = await venueRepository.GetByIdAsync(id, cancellationToken);
        if (venue is null)
        {
            return Result.Failure<VenueDto>(VenueErrors.NotFound(id));
        }

        venue.Name = dto.Name;
        venue.Address = dto.Address;
        venue.MaxCapacity = dto.MaxCapacity;

        await venueRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(venue));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var venue = await venueRepository.GetByIdAsync(id, cancellationToken);
        if (venue is null)
        {
            return Result.Failure(VenueErrors.NotFound(id));
        }

        venue.IsDeleted = true;
        await venueRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static VenueDto ToDto(Venue venue) => new(venue.Id, venue.Name, venue.Address, venue.MaxCapacity);
}
