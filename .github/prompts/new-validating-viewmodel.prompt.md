# Create a Validating ViewModel

Create a StellarUI ViewModel with FluentValidation integration.

## Instructions

Use `ValidatingViewModelBase<T>` when the ViewModel needs reactive input validation.

### Validator

```csharp
using FluentValidation;
using Stellar.FluentValidation;

namespace ${Namespace}.Validators;

public class ${Name}ViewModelValidator : FluentValidatorFor<${Name}ViewModel>
{
    public ${Name}ViewModelValidator()
    {
        // Define validation rules using FluentValidation syntax
        // RuleFor(x => x.Email).NotEmpty().EmailAddress();
        // RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        // RuleFor(x => x.Age).InclusiveBetween(0, 120);
    }
}
```

### ViewModel

```csharp
namespace ${Namespace}.ViewModels;

[ServiceRegistration]
public partial class ${Name}ViewModel : ValidatingViewModelBase<${Name}ViewModel>
{
    [Reactive]
    private string _email;

    [Reactive]
    private string _name;

    // ValidatingViewModelBase provides:
    // - bool IsValid { get; }
    // - ObservableCollection<ValidationInformation> ValidationErrors { get; }

    public ${Name}ViewModel(${Name}ViewModelValidator validator)
        : base(validator)
    {
    }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        // Trigger validation automatically on every PropertyChanged
        this.RegisterValidation()
            .DisposeWith(disposables);

        // Or trigger on a specific observable:
        // this.RegisterValidation(
        //     this.WhenAnyValue(static x => x.Email, static x => x.Name).SelectUnit())
        //     .DisposeWith(disposables);
    }
}
```

### Page Binding for Validation

```csharp
public override void Bind(WeakCompositeDisposable disposables)
{
    // Bind form fields
    this.Bind(ViewModel, static vm => vm.Email, static ui => ui._emailEntry.Text)
        .DisposeWith(disposables);

    // Bind validity to UI
    this.OneWayBind(ViewModel, static vm => vm.IsValid, static ui => ui._submitButton.IsEnabled)
        .DisposeWith(disposables);

    this.OneWayBind(ViewModel, static vm => vm.IsValid, static ui => ui._statusLabel.Text,
        static x => x ? "✓ Valid" : "✗ Invalid")
        .DisposeWith(disposables);

    // Per-property validation feedback
    ViewModel!
        .MonitorValidationInformationFor(static vm => vm.Email)
        .Select(static info => info.IsValid ? string.Empty : info.ErrorMessage ?? string.Empty)
        .BindTo(this, static ui => ui._emailErrorLabel.Text)
        .DisposeWith(disposables);
}
```

## Key Rules
- `ValidatingViewModelBase<T>` where `T` is the ViewModel itself (self-referencing generic)
- Constructor must call `base(validator)`
- `RegisterValidation()` must be called inside `Bind()` and `.DisposeWith(disposables)`
- `IsValid` and `ValidationErrors` are provided by the base class — do not redeclare them
- Validation runs on `RxApp.TaskpoolScheduler` with `ThrottleFirst` debouncing
