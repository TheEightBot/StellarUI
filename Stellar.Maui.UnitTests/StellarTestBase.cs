using Microsoft.Maui.Hosting;

namespace Stellar.Maui.UnitTests;

public abstract class StellarTestBase
{
    // ReactiveUI throws on first use unless it has been built. Every test class goes
    // through the registration an app performs, so the result cannot depend on which
    // class happens to run first.
    static StellarTestBase()
    {
        MauiApp.CreateBuilder(useDefaults: false).UseStellarComponents();
    }
}
