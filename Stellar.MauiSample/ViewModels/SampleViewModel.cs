using System.ComponentModel;
using Stellar.MauiSample.Services;

namespace Stellar.MauiSample.ViewModels;

[ServiceRegistration]
public partial class SampleViewModel(TestService testService)
    : ViewModelBase, ILifecycleEventAware
{
    public TestService TestService { get; } = testService;

    private readonly Guid _id = Guid.NewGuid();

    private long _parameterValue;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goPopup;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goModal;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goValidation;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goNext;

    [Reactive]
    private byte[] _colorArray;

    [Reactive]
    private IEnumerable<TestItem> _testItems;

    [Reactive]
    private TestItem _selectedTestItem;

    [QueryParameter]
    public long ParameterValue
    {
        get => _parameterValue;
        set => this.RaiseAndSetIfChanged(ref _parameterValue, value);
    }

    ~SampleViewModel()
    {
        Console.WriteLine("SimpleSampleViewModel Finalized");
    }

    protected override void Initialize()
    {
        var rng = new Random(Guid.NewGuid().GetHashCode());

        var colors = new byte[4];
        rng.NextBytes(colors);
        ColorArray = colors;

        var items = new List<TestItem>();

        for (int i = 0; i < rng.Next(10, 100); i++)
        {
            items.Add(
                new TestItem
                {
                    Value1 = $"Value {i}",
                    Value2 = i,
                });
        }

        SelectedTestItem = items.ElementAt(rng.Next(0, items.Count - 1));

        TestItems = items;
    }

    protected override void Bind(CompositeDisposable disposables)
    {
        GoPopup =
            ReactiveCommand
                .Create(DefaultAction)
                .DisposeWith(disposables);

        GoModal =
            ReactiveCommand
                .Create(DefaultAction)
                .DisposeWith(disposables);

        GoValidation =
            ReactiveCommand
                .Create(DefaultAction)
                .DisposeWith(disposables);

        GoNext =
            ReactiveCommand
                .Create(DefaultAction)
                .DisposeWith(disposables);
    }

    public void OnLifecycleEvent(LifecycleEvent lifecycleEvent)
    {
        Console.WriteLine($"LifecycleEvent:\t{this._id}\t{lifecycleEvent}");
    }
}

public class TestItem : INotifyPropertyChanged
{
#pragma warning disable CS0067
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    ~TestItem()
    {
        Console.WriteLine("TestItem Finalized");
    }

    public string Value1 { get; set; }

    public int Value2 { get; set; }
}
