namespace Stellar.Maui.UnitTests;

/// <summary>
/// HotReloadService.HotReloadAware is process-wide, so the tests that turn it on run on
/// their own rather than alongside tests that expect it off.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HotReloadCollection
{
    public const string Name = "Hot reload";
}
