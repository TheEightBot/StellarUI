using Stellar.ViewModel;

namespace Stellar.BlazorSample.ViewModels;

[ServiceRegistration]
public partial class CounterViewModel : ViewModelBase
{
    [Reactive]
    private int _count;

    protected override void Bind(WeakCompositeDisposable disposables)
    {
    }
}
