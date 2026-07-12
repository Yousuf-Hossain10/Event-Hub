using EventHub.Application.DTOs;
using FluentValidation;

namespace EventHub.Application.Validators;

public class CreateVenueDtoValidator : AbstractValidator<CreateVenueDto>
{
    public CreateVenueDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MaxCapacity).GreaterThan(0);
    }
}
