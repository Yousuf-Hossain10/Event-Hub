using EventHub.Api.Extensions;
using EventHub.Application.DTOs;
using EventHub.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(
    IBookingService bookingService,
    IValidator<CreateBookingDto> createValidator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto, CancellationToken cancellationToken)
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

        var result = await bookingService.CreateAsync(dto, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToErrorActionResult(result.Error);
        }

        return Created($"/api/bookings/{result.Value.Id}", result.Value);
    }
}
