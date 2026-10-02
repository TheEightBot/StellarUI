using System.Reactive;
using Stellar.ViewModel;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class CommandCellViewModel : ViewModelBase
{
    private ReactiveCommand<Unit, Unit>? _select;

    public ReactiveCommand<Unit, Unit>? Select
    {
        get => _select;
        private set => this.RaiseAndSetIfChanged(ref _select, value);
    }

    public int SelectCalls { get; private set; }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        Select =
            ReactiveCommand
                .Create(() => { SelectCalls++; })
                .DisposeWith(disposables);
    }
}
