namespace Stellar;

public interface IProvideValidation<TNeedsValidation>
    where TNeedsValidation : class
{
    ValidationResult Validate(TNeedsValidation validation);
}