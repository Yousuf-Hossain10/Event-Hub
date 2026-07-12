using EventHub.Application.DTOs;
using FluentValidation;

namespace EventHub.Application.Validators;

public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
{
    public UpdateEventDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.VenueId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
