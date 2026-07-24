namespace Stellar.UnitTests;

/// <summary>
/// Helpers for the tests that exercise weak-reference behaviour.
/// </summary>
internal static class GcHelpers
{
    /// <summary>
    /// Forces a full blocking collection. Repeated because a single pass does not
    /// reliably clear entries whose finalization frees further references.
    /// </summary>
    public static void ForceFullCollection()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
        }
    }
}
