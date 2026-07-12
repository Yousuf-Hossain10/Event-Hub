using EventHub.Application.DTOs;
using FluentValidation;

namespace EventHub.Application.Validators;

public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingDtoValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.AttendeeId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
