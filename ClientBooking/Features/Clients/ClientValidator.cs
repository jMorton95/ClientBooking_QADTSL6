using FluentValidation;

namespace ClientBooking.Features.Clients;

public class ClientValidator : AbstractValidator<ClientRequest>
{
    //Validation rules for creating a client
    //Name and email must follow the Maximum Length constraint applied to their database table
    public ClientValidator()
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