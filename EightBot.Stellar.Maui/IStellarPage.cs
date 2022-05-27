namespace EightBot.Stellar.Maui;

public interface IStellarPage<TViewModel> : IStellarPage, IStellarView<TViewModel>
    where TViewModel : class
{
}

public interface IStellarPage : IStellarView
{
    IObservable<Unit> IsAppearing { get; }

    IObservable<Unit> IsDisappearing { get; }
}