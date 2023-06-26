using FluentValidation;

namespace MinimalApisVsControllers;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}