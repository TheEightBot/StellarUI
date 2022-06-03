using EightBot.Stellar.MauiSample.Services;

namespace EightBot.Stellar.MauiSample.UserInterface.Pages;

[PreCache]
[ServiceRegistration]
public class SamplePage : ContentPageBase<ViewModels.SampleViewModel>
{
    private VerticalStackLayout _mainLayout;

    private Button _next;

    private BoxView _color;

    private Picker _picker;

    private ListView _listView;

    public SamplePage(ViewModels.SampleViewModel sampleViewModel)
    {
        this.ViewModel = sampleViewModel;
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
                    new Button
                    {
                        Text = "Next",
                        HeightRequest = 32,
                    }
                        .Assign(out _next),
                    new BoxView
                    {
                        HeightRequest = 60,
                    }
                        .Assign(out _color),
                    new Picker
                    {
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
        this.BindCommand(ViewModel, vm => vm.GoNext, ui => ui._next, Observables.UnitDefault)
            .DisposeWith(ControlBindings);

        this.WhenAnyObservable(x => x.ViewModel.GoNext)
            .NavigateToPage<Unit, SamplePage>(this)
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