namespace Stellar;

public interface IProvideValidation<TNeedsValidation>
    where TNeedsValidation : class
{
    ValidationResult PerformValidate(TNeedsValidation validation);
}
