using Microsoft.Maui.Hosting;

namespace Stellar.Maui.UnitTests;

public abstract class StellarTestBase
{
    static StellarTestBase()
    {
        MauiApp.CreateBuilder(useDefaults: false).UseStellarComponents();
    }
}
