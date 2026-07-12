using EventHub.Application.DTOs;
using EventHub.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController(
    IVenueService venueService,
    IValidator<CreateVenueDto> createValidator,
    IValidator<UpdateVenueDto> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VenueDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await venueService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var venue = await venueService.GetByIdAsync(id, cancellationToken);
        return venue is null ? NotFound() : Ok(venue);
    }

    [HttpPost]
    public async Task<ActionResult<VenueDto>> Create(CreateVenueDto dto, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var venue = await venueService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = venue.Id }, venue);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VenueDto>> Update(Guid id, UpdateVenueDto dto, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var venue = await venueService.UpdateAsync(id, dto, cancellationToken);
        return venue is null ? NotFound() : Ok(venue);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await venueService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
