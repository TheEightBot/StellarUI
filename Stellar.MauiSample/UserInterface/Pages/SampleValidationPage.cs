using Stellar.MauiSample.ViewModels;

namespace Stellar.MauiSample.UserInterface.Pages;

[ServiceRegistration]
public class SampleValidationPage : ContentPageBase<ViewModels.SampleValidationViewModel>
{
    private VerticalStackLayout _mainLayout;

    private Entry _value;

    private Label _valueLabel;

    private Label _isValidLabel;

    private BoxView _color;

    public SampleValidationPage(SampleValidationViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void SetupUserInterface()
    {
        Content =
            new VerticalStackLayout
            {
                Padding = 8,
                Spacing = 8,
                Children =
                {
                    new Entry
                    {
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Assign(out _value),
                    new Label
                    {
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Assign(out _valueLabel),
                    new Label
                    {
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Assign(out _isValidLabel),
                    new BoxView
                    {
                        HeightRequest = 60,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Assign(out _color),
                },
            }
                .Assign(out _mainLayout);
    }

    public override void Bind(CompositeDisposable disposables)
    {
        this.Bind(ViewModel, static vm => vm.StringValue, static ui => ui._value.Text)
            .DisposeWith(disposables);

        this.WhenAnyValue(static x => x.ViewModel.StringValue)
            .Select(static x => $"Value: '{x}'")
            .BindTo(this, static ui => ui._valueLabel.Text)
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, static vm => vm.IsValid, static ui => ui._isValidLabel.Text, x => $"Is Valid: {x}")
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, static vm => vm.IsValid, static ui => ui._color.Color, static x => x ? Colors.Chartreuse : Colors.Fuchsia)
            .DisposeWith(disposables);
    }
}
