using System.Reactive;
using Stellar.ViewModel;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class TestCellViewModel : ViewModelBase, ILifecycleEventAware
{
    private TestItem? _item;
    private ReactiveCommand<Unit, Unit>? _select;

    public TestCellViewModel(LongLivedSource? source = null)
    {
        Source = source;
    }

    public LongLivedSource? Source { get; }

    public TestItem? Item
    {
        get => _item;
        set => this.RaiseAndSetIfChanged(ref _item, value);
    }

    public ReactiveCommand<Unit, Unit>? Select
    {
        get => _select;
        private set => this.RaiseAndSetIfChanged(ref _select, value);
    }

    public int BindCalls { get; private set; }

    public int SelectCalls { get; private set; }

    public List<LifecycleEvent> Events { get; } = new();

    public void OnLifecycleEvent(LifecycleEvent lifecycleEvent) => Events.Add(lifecycleEvent);

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        BindCalls++;

        Select =
            ReactiveCommand
                .Create(() => { SelectCalls++; })
                .DisposeWith(disposables);
    }
}
