using ReactiveUI.Reactive.Maui;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class ParameterView : ReactiveContentView<CommandCellViewModel>
{
    private string _parameter = "first";

    public Button Select { get; } = new();

    public string Parameter
    {
        get => _parameter;
        set
        {
            _parameter = value;
            OnPropertyChanged();
        }
    }
}
