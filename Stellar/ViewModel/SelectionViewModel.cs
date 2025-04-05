namespace Stellar.ViewModel;

public partial class SelectionViewModel<TSelectedValKey> : ViewModelBase
{
    [Reactive]
    public TSelectedValKey? Key { get; set; }

    [Reactive]
    public string? DisplayValue { get; set; }

    [Reactive]
    public bool Selected { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, bool>? ToggleSelected { get; private set; }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        ToggleSelected =
            ReactiveCommand
                .Create(() => Selected = !Selected)
                .DisposeWith(disposables);
    }
}
