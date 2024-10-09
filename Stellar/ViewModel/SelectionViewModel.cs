using System;
using System.Runtime.CompilerServices;

namespace Stellar.ViewModel;

public partial class SelectionViewModel<TSelectedValKey> : ViewModelBase
{
    [Reactive]
    private TSelectedValKey? _key;

    [Reactive]
    public string? _displayValue;

    [Reactive]
    public bool _selected;

    [Reactive]
    private ReactiveCommand<Unit, bool>? _toggleSelected;

    protected override void Bind(CompositeDisposable disposables)
    {
        ToggleSelected =
            ReactiveCommand
                .Create(() => Selected = !Selected)
                .DisposeWith(disposables);
    }
}
