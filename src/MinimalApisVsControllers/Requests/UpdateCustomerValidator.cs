using FluentValidation;

namespace MinimalApisVsControllers;

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}