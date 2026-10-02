using System.Reactive.Linq;

namespace Stellar.Maui.UnitTests.Support;

/// <summary>
/// The bindings every test cell registers, kept in one place so the cells differ only in
/// the Stellar base class they derive from.
/// </summary>
internal static class TestCellBindings
{
    public static void Bind<TCell>(TCell cell, WeakCompositeDisposable disposables)
        where TCell : class, ITestCell
    {
        cell.WhenAnyValue(static x => x.ViewModel!.Item)
            .IsNotNull()
            .Subscribe(item => cell.Name.Text = item.Name)
            .DisposeWith(disposables);

        Observable
            .CombineLatest(
                cell.WhenAnyValue(static x => x.ViewModel!.Item).IsNotNull(),
                cell.WhenAnyValue(static x => x.ViewModel!.Source!.Unit),
                static (item, unit) => $"{item.Amount} {unit}")
            .Subscribe(text => cell.Amount.Text = text)
            .DisposeWith(disposables);

        cell.BindCommand(cell.ViewModel, static vm => vm.Select, static ui => ui.Select)
            .DisposeWith(disposables);
    }
}
