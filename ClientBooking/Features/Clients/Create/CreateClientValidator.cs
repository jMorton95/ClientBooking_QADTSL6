using ClientBooking.Features.Clients.Shared;
using FluentValidation;

namespace ClientBooking.Features.Clients.Create;

public class CreateClientValidator : AbstractValidator<ClientRequest>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Client name is required.")
            .MaximumLength(255).WithMessage("Client name cannot exceed 255 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}