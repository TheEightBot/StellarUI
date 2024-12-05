using FluentValidation;

namespace Stellar.FluentValidation;

public abstract class FluentValidatorFor<TNeedsValidation> : AbstractValidator<TNeedsValidation>, IProvideValidation<TNeedsValidation>
    where TNeedsValidation : class
{
    ValidationResult IProvideValidation<TNeedsValidation>.PerformValidate(TNeedsValidation validation)
    {
        var result = this.Validate(validation);

        return new(
            result.Errors
                    .Select(err =>
                        new ValidationInformation(err.PropertyName, err.ErrorMessage, err.AttemptedValue)
                        {
                            ErrorCode = err.ErrorCode,
                        })
                    .ToList(),
            result.IsValid);
    }
}
