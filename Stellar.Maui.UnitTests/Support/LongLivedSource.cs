using System.ComponentModel;

namespace Stellar.Maui.UnitTests.Support;

/// <summary>
/// Stands in for a singleton a cell observes, such as a repository. Anything still
/// subscribed to it is rooted for as long as it lives, which is what makes a leaked
/// subscription visible here as a handler that was never removed.
/// </summary>
internal sealed class LongLivedSource : INotifyPropertyChanged
{
    private string _unit = "ft";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Unit
    {
        get => _unit;
        set
        {
            _unit = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Unit)));
        }
    }

    public int HandlerCount => PropertyChanged?.GetInvocationList().Length ?? 0;
}
