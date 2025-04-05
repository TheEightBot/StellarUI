using System;
using System.Reactive.Disposables;
using Stellar.MauiSample.ViewModels;

namespace Stellar.MauiSample.UserInterface.Pages;

[ServiceRegistration]
public class SamplePopupPage : PopupPageBase<SampleViewModel>
{
    private VerticalStackLayout _mainLayout;

    private Button _close;

    private BoxView _color;

    private Picker _picker;

    private ListView _listView;

    public SamplePopupPage(SampleViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
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

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, static vm => vm.GoNext, static ui => ui._close, Observables.UnitDefault)
            .DisposeWith(disposables);

        this.WhenAnyObservable(static x => x.ViewModel.GoNext)
            .NavigatePopPopupPage()
            .DisposeWith(disposables);

        this.WhenAnyValue(static x => x.ViewModel.ColorArray)
            .IsNotNull()
            .Select(static colorArray => new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]))
            .BindTo(this, static ui => ui._color.BackgroundColor)
            .DisposeWith(disposables);

        _picker
            .BindPicker(
                this.WhenAnyValue(static x => x.ViewModel.TestItems),
                x => this.ViewModel.SelectedTestItem = x,
                x => this.ViewModel.SelectedTestItem == x,
                x => x.Value1)
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, static vm => vm.TestItems, static ui => ui._listView.ItemsSource)
            .DisposeWith(disposables);
    }
}
