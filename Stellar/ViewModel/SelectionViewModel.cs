namespace Stellar.ViewModel;

public partial class SelectionViewModel<TSelectedValKey> : ViewModelBase
{
    [Reactive]
    public partial TSelectedValKey? Key { get; set; }

    [Reactive]
    public partial string? DisplayValue { get; set; }

    [Reactive]
    public partial bool Selected { get; set; }

    [Reactive]
    public partial ReactiveCommand<Unit, bool>? ToggleSelected { get; private set; }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        ToggleSelected =
            ReactiveCommand
                .Create(() => Selected = !Selected)
                .DisposeWith(disposables);
    }
}
