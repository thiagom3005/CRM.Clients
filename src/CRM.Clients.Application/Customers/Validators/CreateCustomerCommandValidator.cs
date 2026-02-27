using CRM.Clients.Application.Customers.Commands;
using CRM.Clients.Domain.Aggregates.Customer;
using FluentValidation;

namespace CRM.Clients.Application.Customers.Validators;

/// <summary>
/// Valida shape basico do comando antes de chegar ao handler.
/// Nao duplica regras do dominio (idade, IE/isencao) -- essas ficam no aggregate.
/// </summary>
public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Document)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Phone)
            .NotEmpty();

        RuleFor(x => x.ZipCode)
            .NotEmpty();

        RuleFor(x => x.Street)
            .NotEmpty();

        RuleFor(x => x.City)
            .NotEmpty();

        RuleFor(x => x.State)
            .NotEmpty()
            .Length(2);

        // IE obrigatoria para PJ nao isenta -- validacao antecipada de shape.
        When(
            x => x.Type == CustomerType.Company && !x.IsStateRegistrationExempt,
            () => RuleFor(x => x.StateRegistration).NotEmpty());
    }
}
