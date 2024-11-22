using System.Reactive.Subjects;
using ReactiveUI;

namespace Stellar.Blazor;

public class BlazorViewManager<TViewModel> : ViewManager<TViewModel>
    where TViewModel : class
{
}
