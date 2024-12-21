using ReactiveMarbles.ObservableEvents;
using Stellar.MauiSample.UserInterface.Views;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace Stellar.MauiSample.UserInterface.Pages;

[ServiceRegistration]
public class SimpleSamplePage : ContentPageBase<ViewModels.SampleViewModel>
{
    private Grid _mainLayout;

    private Button _popup;

    private Button _modal;

    private Button _next;

    private Button _collect;

    private Button _validation;

    private Label _parameterValue;

    private Picker _picker;

    private ListView _listView;

    public SimpleSamplePage()
    {
        this.InitializeStellarComponent();
    }

    ~SimpleSamplePage()
    {
        Console.WriteLine("GCing simple sample page");
    }

    public override void SetupUserInterface()
    {
        Content =
            new Grid
            {
                ColumnDefinitions = Columns.Define([Star]),
                RowDefinitions = Rows.Define([Auto, Auto, Auto, Auto, Auto, Auto, Auto, Star]),
                Padding = 8,
                RowSpacing = 8,
                Children =
                {
                    new Button
                    {
                        Text = "Popup",
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(0).Column(0)
                        .Assign(out _popup),
                    new Button
                    {
                        Text = "Modal",
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(1).Column(0)
                        .Assign(out _modal),
                    new Button
                    {
                        Text = "Validation",
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(2).Column(0)
                        .Assign(out _validation),
                    new Button
                    {
                        Text = "Next",
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(3).Column(0)
                        .Assign(out _next),
                    new Button
                    {
                        Text = "Collect",
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(4).Column(0)
                        .Assign(out _collect),
                    new Label
                    {
                        HeightRequest = 32,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(5).Column(0)
                        .Assign(out _parameterValue),
                    new SampleView
                    {
                        HeightRequest = 60,
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(6).Column(0),
                    new Picker
                    {
                        VerticalOptions = LayoutOptions.Start,
                    }
                        .Row(7).Column(0)
                        .Assign(out _picker),
                    new ListView
                    {
                        ItemTemplate = new DataTemplate(typeof(Cells.SampleViewCell)),
                        VerticalOptions = LayoutOptions.Fill,
                    }
                        .Row(8).Column(0)
                        .Assign(out _listView),
                },
            }
                .Assign(out _mainLayout);
    }

    public override void Bind(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, static vm => vm.GoNext, static ui => ui._next, Observables.UnitDefault)
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, static vm => vm.ParameterValue, static ui => ui._parameterValue.Text, x => $"Parameter: {x}")
            .DisposeWith(disposables);

        this.WhenAnyObservable(static x => x.ViewModel.GoNext)
            .Select(static _ => 0)
            .NavigateToPage<int, SimpleSamplePage>(
                this,
                queryParameters: static (value, dict) =>
                {
                    dict.Add("ParameterValue", value);
                })
            .DisposeWith(disposables);

        this.BindCommand(ViewModel, static vm => vm.GoPopup, static ui => ui._popup, Observables.UnitDefault)
            .DisposeWith(disposables);

        this.WhenAnyObservable(static x => x.ViewModel.GoPopup)
            .NavigateToPopupPage<SamplePopupPage>()
            .DisposeWith(disposables);

        this.BindCommand(ViewModel, static vm => vm.GoModal, static ui => ui._modal, Observables.UnitDefault)
            .DisposeWith(disposables);

        this.WhenAnyObservable(static x => x.ViewModel.GoModal)
            .NavigateToModalPage<SampleModalPage>(this)
            .DisposeWith(disposables);

        this.BindCommand(ViewModel, static vm => vm.GoValidation, static ui => ui._validation, Observables.UnitDefault)
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, static x => x.TestItems, static ui => ui._listView.ItemsSource)
            .DisposeWith(disposables);

        this.WhenAnyObservable(static x => x.ViewModel.GoValidation)
            .NavigateToPage<SampleValidationPage>(this)
            .DisposeWith(disposables);

        _collect.Events()
            .Clicked
            .Do(
                _ =>
                {
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                })
            .Subscribe()
            .DisposeWith(disposables);

        _picker
            .Bind(
                this.WhenAnyValue(static x => x.ViewModel.TestItems),
                x => this.ViewModel.SelectedTestItem = x,
                x => this.ViewModel.SelectedTestItem == x,
                x => x.Value1)
            .DisposeWith(disposables);
    }
}
