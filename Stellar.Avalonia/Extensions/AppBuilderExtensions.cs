using System.Reflection;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Reactive.Builder;
using Splat;

namespace Stellar.Avalonia;

public static class AppBuilderExtensions
{
    public static AppBuilder UseStellarComponents(this AppBuilder appBuilder)
    {
        // Avalonia.ReactiveUI has no release compatible with ReactiveUI 24 (or with
        // Avalonia 12), so Stellar no longer references it and provides the pieces it
        // needs itself: Stellar's own AvaloniaActivationForViewFetcher and
        // AvaloniaScheduler here, and the IViewFor implementations in
        // WindowBase/UserControlBase. Avalonia.ReactiveUI's AutoDataTemplateBindingHook
        // is intentionally not re-implemented: it only supplied automatic data
        // templates for ViewModelViewHost, which Stellar's view management never uses.
        Locator.CurrentMutable.InitializeSplat();

        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithRegistration(
                static resolver => resolver.RegisterConstant<IActivationForViewFetcher>(new AvaloniaActivationForViewFetcher()))
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
