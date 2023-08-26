using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Stellar.AvaloniaSample.ViewModels;
using Stellar.AvaloniaSample.Views;

namespace Stellar.AvaloniaSample;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new StellarMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}