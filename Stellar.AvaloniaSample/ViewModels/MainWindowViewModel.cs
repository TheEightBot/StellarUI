using Stellar.ViewModel;

namespace Stellar.AvaloniaSample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [Reactive]
    private string _greeting = "Welcome to Avalonia!";

    protected override void Bind(CompositeDisposable disposables)
    {
    }
}
