namespace EightBot.Stellar;

public static class Observables
{
    public static readonly IObservable<Unit> UnitDefault = Observable.Return(Unit.Default);
}