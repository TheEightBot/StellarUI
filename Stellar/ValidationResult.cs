using ReactiveUI.Fody.Helpers;

namespace Stellar;

public record ValidationResult
{
    public bool IsValid { get; }

    public ICollection<ValidationInformation> ValidationInformation { get; }

    public ValidationResult(ICollection<ValidationInformation> validationInformation, bool isValid)
    {
        ValidationInformation = validationInformation;
        IsValid = isValid;
    }

    public static ValidationResult DefaultValidationResult = new ValidationResult(new List<ValidationInformation>(), true);
}

public record ValidationInformation
{
    public string PropertyName { get; }

    public bool IsError { get; }

    public object? AttemptedValue { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public ValidationInformation(string propertyName, bool isError = false)
    {
        PropertyName = propertyName;
        IsError = isError;
    }

    public ValidationInformation(string propertyName, string error)
        : this(propertyName, error, null)
    {
    }

    public ValidationInformation(string propertyName, string error, object? attemptedValue)
    {
        PropertyName = propertyName;
        ErrorMessage = error;
        AttemptedValue = attemptedValue;
        IsError = false;
    }
}
