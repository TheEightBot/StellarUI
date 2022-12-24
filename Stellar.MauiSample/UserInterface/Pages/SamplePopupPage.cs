using System;
using Stellar.MauiSample.ViewModels;

namespace Stellar.MauiSample.UserInterface.Pages;

[ServiceRegistration(Lifetime.Singleton)]
public class SamplePopupPage : PopupPageBase<SampleViewModel>
{
    private VerticalStackLayout _mainLayout;

    private Button _close;

    private BoxView _color;

    private Picker _picker;

    private ListView _listView;

    public SamplePopupPage()
    {
        this.InitializeComponent();
    }

    public override void SetupUserInterface()
    {
        this.HasKeyboardOffset = false;

        Content =
            new VerticalStackLayout
            {
                Padding = 8,
                Spacing = 8,
                VerticalOptions = LayoutOptions.Fill,
                BackgroundColor = Colors.Fuchsia,
                Children =
                {
                    new Button
                    {
                        Text = "Close",
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Assign(out _close),
                    new BoxView
                    {
                        HeightRequest = 60,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Assign(out _color),
                    new Picker
                    {
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Assign(out _picker),

                    new ListView
                    {
                        ItemTemplate = new DataTemplate(typeof(Cells.SampleViewCell)),
                        VerticalOptions = LayoutOptions.Fill,
                    }
                        .Assign(out _listView),
                },
            }
                .Assign(out _mainLayout);
    }

    public override void BindControls()
    {
        this.BindCommand(ViewModel, vm => vm.GoNext, ui => ui._close, Observables.UnitDefault)
        .DisposeWith(ControlBindings);

        this.WhenAnyObservable(x => x.ViewModel.GoNext)
            .NavigatePopPopupPage()
            .DisposeWith(ControlBindings);

        this.WhenAnyValue(x => x.ViewModel.ColorArray)
            .IsNotNull()
            .Select(colorArray => new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]))
            .BindTo(this, ui => ui._color.BackgroundColor)
            .DisposeWith(ControlBindings);

        _picker
            .Bind(
                this.WhenAnyValue(x => x.ViewModel.TestItems),
                x => this.ViewModel.SelectedTestItem = x,
                x => this.ViewModel.SelectedTestItem == x,
                x => x.Value1)
            .DisposeWith(ControlBindings);

        this.OneWayBind(ViewModel, vm => vm.TestItems, ui => ui._listView.ItemsSource)
            .DisposeWith(ControlBindings);
    }
}