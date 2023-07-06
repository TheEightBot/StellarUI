using System;
using Stellar.MauiSample.ViewModels;

namespace Stellar.MauiSample.UserInterface.Pages;

[ServiceRegistration(Lifetime.Singleton)]
public class SampleModalPage : ContentPageBase<SampleViewModel>
{
    private VerticalStackLayout _mainLayout;

    private Button _close;

    private BoxView _color;

    private Picker _picker;

    private ListView _listView;

    public SampleModalPage()
    {
        this.InitializeStellarComponent();
    }

    public override void SetupUserInterface()
    {
        // this.BackgroundColor = Colors.Transparent;
        this.BackgroundColor = Color.FromRgba(0d, 0d, 0d, 0.0000001d);

        Content =
            new VerticalStackLayout
            {
                Margin = 32,
                Padding = 8,
                Spacing = 8,
                BackgroundColor = Colors.Chartreuse,
                VerticalOptions = LayoutOptions.Fill,
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

    public override void Bind(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, vm => vm.GoNext, ui => ui._close, Observables.UnitDefault)
            .DisposeWith(disposables);

        this.WhenAnyObservable(x => x.ViewModel.GoNext)
            .NavigatePopModalPage(this)
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel.ColorArray)
            .IsNotNull()
            .Select(colorArray => new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]))
            .BindTo(this, ui => ui._color.BackgroundColor)
            .DisposeWith(disposables);

        _picker
            .Bind(
                this.WhenAnyValue(x => x.ViewModel.TestItems),
                x => this.ViewModel.SelectedTestItem = x,
                x => this.ViewModel.SelectedTestItem == x,
                x => x.Value1)
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, vm => vm.TestItems, ui => ui._listView.ItemsSource)
            .DisposeWith(disposables);
    }
}