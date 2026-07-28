using FluentValidation;

namespace Stellar.FluentValidation;

public abstract class FluentValidatorFor<TNeedsValidation> : AbstractValidator<TNeedsValidation>, IProvideValidation<TNeedsValidation>
    where TNeedsValidation : class
{
    ValidationResult IProvideValidation<TNeedsValidation>.Validate(TNeedsValidation validation)
    {
        var result = this.Validate(validation);
        var errors = result.Errors;

        // Validation runs on every change to the watched properties, and the overwhelming
        // majority of those passes are valid. Returning the shared empty result keeps that
        // path allocation-free instead of building an empty list each time.
        if (errors.Count == 0)
        {
            return result.IsValid
                ? ValidationResult.DefaultValidationResult
                : new(Array.Empty<ValidationInformation>(), false);
        }

        // Exactly-sized array rather than Select().ToList(): no LINQ enumerator, and no
        // repeated growth as the list fills.
        var information = new ValidationInformation[errors.Count];

        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];

            information[i] =
                new ValidationInformation(error.PropertyName, error.ErrorMessage, error.AttemptedValue)
                {
                    ErrorCode = error.ErrorCode,
                };
        }

        return new(information, result.IsValid);
    }
}
