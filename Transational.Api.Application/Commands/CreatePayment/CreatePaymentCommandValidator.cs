using FluentValidation;

namespace Transational.Api.Application.Commands.CreatePayment;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required")
            .NotEqual(Guid.Empty)
            .WithMessage("CustomerId must be a valid GUID");

        RuleFor(x => x.ServiceProviderId)
            .NotEmpty()
            .WithMessage("ServiceProviderId is required")
            .NotEqual(Guid.Empty)
            .WithMessage("ServiceProviderId must be a valid GUID");

        RuleFor(x => x.PaymentMethodId)
            .GreaterThan(0)
            .WithMessage("PaymentMethodId must be greater than 0");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");
    }
}
