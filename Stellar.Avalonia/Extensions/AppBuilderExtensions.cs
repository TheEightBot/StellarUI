using System.Reflection;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Builder;
using Splat;

namespace Stellar.Avalonia;

public static class AppBuilderExtensions
{
    public static AppBuilder UseStellarComponents(this AppBuilder appBuilder)
    {
        // Avalonia.ReactiveUI 11.3.9 was built against ReactiveUI 20 and its
        // bootstrap types -- Registrations and AvaloniaMixins.UseReactiveUI --
        // reference RxApp, PlatformRegistrationManager and Splat's IEnableLogger,
        // all removed in ReactiveUI 23 / Splat 19. Those types now fail to load,
        // so Stellar performs the registrations itself. The pieces it needs
        // (AvaloniaActivationForViewFetcher, AvaloniaScheduler,
        // AutoDataTemplateBindingHook, and the ReactiveWindow/ReactiveUserControl
        // base classes) all load and work against ReactiveUI 23.
        Locator.CurrentMutable.InitializeSplat();

        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithRegistration(
                static resolver =>
                {
                    resolver.RegisterConstant<IActivationForViewFetcher>(new AvaloniaActivationForViewFetcher());
                    resolver.RegisterConstant<IPropertyBindingHook>(new AutoDataTemplateBindingHook());
                })
            .WithMainThreadScheduler(AvaloniaScheduler.Instance)
            .WithTaskPoolScheduler(Schedulers.ShortTermThreadPoolScheduler)
            .Build();

        return appBuilder;
    }

    public static AppBuilder EnableHotReload(this AppBuilder appBuilder)
    {
        HotReloadService.HotReloadAware = true;

        return appBuilder;
    }

    public static AppBuilder PreCacheComponents<TStellarAssembly>(this AppBuilder appBuilder)
    {
        PreCache(appBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return appBuilder;
    }

    private static Task PreCache(AppBuilder appBuilder, Assembly assembly)
    {
        if (assembly is null)
        {
            return Task.CompletedTask;
        }

        return Task.Run(
            () =>
            {
                var precacheAttribute = typeof(PreCacheAttribute);

                var assTypes =
                    assembly
                        ?.ExportedTypes
                        ?.Where(
                            ti =>
                                Attribute.IsDefined(ti, precacheAttribute) &&
                                ti.IsClass && !ti.IsAbstract &&
                                ti.GetConstructor(Type.EmptyTypes) is not null && !ti.ContainsGenericParameters)
                    ?? Enumerable.Empty<Type>();

                foreach (var ti in assTypes)
                {
                    if (ti.FullName is not null)
                    {
                        assembly!.CreateInstance(ti.FullName);
                    }
                }
            });
    }
}
