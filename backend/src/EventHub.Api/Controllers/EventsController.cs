using EventHub.Api.Extensions;
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
        var result = await eventService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorActionResult(result.Error);
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

        var result = await eventService.CreateAsync(dto, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToErrorActionResult(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
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

        var result = await eventService.UpdateAsync(id, dto, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorActionResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await eventService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToErrorActionResult(result.Error);
    }
}
