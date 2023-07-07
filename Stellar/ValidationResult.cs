using ReactiveUI.Fody.Helpers;

namespace Stellar;

public record ValidationResult
{
    public bool IsValid { get; set; }

    public ICollection<ValidationInformation> ValidationInformation { get; set; }
}

public record ValidationInformation
{
    public object AttemptedValue { get; set; }

    public string ErrorCode { get; set; }

    public string ErrorMessage { get; set; }

    public string PropertyName { get; set; }

    public bool IsError { get; set; }

    public ValidationInformation(string propertyName, bool isValid = true)
    {
        this.PropertyName = propertyName;
        this.IsError = isValid;
    }

    public ValidationInformation(string propertyName, string error)
        : this(propertyName, error, null)
    {
    }

    public ValidationInformation(string propertyName, string error, object attemptedValue)
    {
        this.PropertyName = propertyName;
        this.ErrorMessage = error;
        this.AttemptedValue = attemptedValue;
        this.IsError = false;
    }
}