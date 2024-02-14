#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Stellar.Avalonia.HotReloadService))]

namespace Stellar.Avalonia;

public static class HotReloadService
{
    public static bool HotReloadAware { get; set; }

    public static event Action<Type[]?>? UpdateApplicationEvent;

    internal static void ClearCache(Type[]? types)
    {
    }

    internal static void UpdateApplication(Type[]? types)
    {
        UpdateApplicationEvent?.Invoke(types);
    }
}
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.