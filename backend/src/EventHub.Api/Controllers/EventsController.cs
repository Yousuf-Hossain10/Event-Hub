using EventHub.Application.Common;
using EventHub.Application.DTOs;
using EventHub.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(
    IEventService eventService,
    IValidator<CreateEventDto> createValidator,
    IValidator<UpdateEventDto> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await eventService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var @event = await eventService.GetByIdAsync(id, cancellationToken);
        return @event is null ? NotFound() : Ok(@event);
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> Create(CreateEventDto dto, CancellationToken cancellationToken)
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

        try
        {
            var created = await eventService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (VenueNotFoundException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventDto>> Update(Guid id, UpdateEventDto dto, CancellationToken cancellationToken)
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

        try
        {
            var updated = await eventService.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (VenueNotFoundException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await eventService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
