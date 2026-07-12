using EventHub.Application.DTOs;
using FluentValidation;

namespace EventHub.Application.Validators;

public class UpdateVenueDtoValidator : AbstractValidator<UpdateVenueDto>
{
    public UpdateVenueDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MaxCapacity).GreaterThan(0);
    }
}
