using FluentValidation;
using Stellar.FluentValidation;
using Stellar.MauiSample.ViewModels;

namespace Stellar.MauiSample.Validators;

[ServiceRegistration(Lifetime.Singleton)]
public class SampleValidationViewModelValidator : FluentValidatorFor<SampleValidationViewModel>
{
    public SampleValidationViewModelValidator()
    {
        RuleFor(x => x.StringValue)
            .NotNull()
            .NotEmpty();
    }
}