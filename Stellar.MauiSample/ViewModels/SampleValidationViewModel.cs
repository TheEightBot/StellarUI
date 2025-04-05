using Stellar.MauiSample.Validators;

namespace Stellar.MauiSample.ViewModels;

[ServiceRegistration]
public partial class SampleValidationViewModel : ValidatingViewModelBase<SampleValidationViewModel>
{
    [Reactive]
    public string _stringValue;

    public SampleValidationViewModel(SampleValidationViewModelValidator validator)
        : base(validator)
    {
    }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        this.WhenAnyValue(static x => x.StringValue)
            .Do(static x => System.Diagnostics.Debug.WriteLine($"String Value: {x}"))
            .Subscribe()
            .DisposeWith(disposables);

        this.RegisterValidation()
           .DisposeWith(disposables);
    }
}
