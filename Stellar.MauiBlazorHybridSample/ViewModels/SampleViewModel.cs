using System.ComponentModel;
using Stellar.MauiSample.Services;

namespace Stellar.MauiBlazorHybridSample.ViewModels;

// CA2213 cannot trace disposal through WeakCompositeDisposable: every command below is
// created in Bind and handed to the disposables the framework passes in, which tears them
// down when bindings are unregistered.
#pragma warning disable CA2213

[ServiceRegistration]
public partial class SampleViewModel(TestService testService)
    : ViewModelBase, ILifecycleEventAware
{
    public TestService TestService { get; } = testService;

    private readonly Guid _id = Guid.NewGuid();

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goPopup = null!;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goModal = null!;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goValidation = null!;

    [Reactive]
    private ReactiveCommand<Unit, Unit> _goNext = null!;

    [Reactive]
    private byte[] _colorArray = null!;

    [Reactive]
    private IEnumerable<TestItem> _testItems = null!;

    [Reactive]
    private TestItem _selectedTestItem = null!;

    [Reactive]
    [property: QueryParameter]
    private long _parameterValue;

    // The finalizer exists so the sample can show the view model actually being
    // collected. CA1063 requires it to do nothing but hand off to Dispose(false), so the
    // logging moves into the dispose path.
    ~SampleViewModel() => Dispose(false);

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            Console.WriteLine("SimpleSampleViewModel Finalized");
        }

        base.Dispose(disposing);
    }

    protected override void Initialize()
    {
        var rng = new Random(Guid.NewGuid().GetHashCode());

        var colors = new byte[4];
        rng.NextBytes(colors);
        this.ColorArray = colors;

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

        this.SelectedTestItem = items.ElementAt(rng.Next(0, items.Count - 1));

        this.TestItems = items;
    }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        this.GoPopup =
            ReactiveCommand
                .Create(DefaultAction)
                .DisposeWith(disposables);

        this.GoModal =
            ReactiveCommand
                .Create(DefaultAction)
                .DisposeWith(disposables);

        this.GoValidation =
            ReactiveCommand
                .Create(DefaultAction)
                .DisposeWith(disposables);

        this.GoNext =
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
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    ~TestItem()
    {
        Console.WriteLine("TestItem Finalized");
    }

    public string Value1 { get; set; } = string.Empty;

    public int Value2 { get; set; }
}

#pragma warning restore CA2213
