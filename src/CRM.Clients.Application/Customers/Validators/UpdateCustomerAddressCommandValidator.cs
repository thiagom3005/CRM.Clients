using CRM.Clients.Application.Customers.Commands;
using FluentValidation;

namespace CRM.Clients.Application.Customers.Validators;

public sealed class UpdateCustomerAddressCommandValidator : AbstractValidator<UpdateCustomerAddressCommand>
{
    public UpdateCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId)
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
    }
}
