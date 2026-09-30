namespace Stellar.Maui.UnitTests.Support;

/// <summary>
/// How a platform gives a view that has just come out of the template to its list.
/// </summary>
public enum FirstBind
{
    /// <summary>
    /// iOS and Windows: the view becomes a child of the list and takes its window from it.
    /// </summary>
    AddToList,

    /// <summary>
    /// Android: the list's window is propagated to the view first. The view has no parent
    /// yet, so it immediately resolves its window from that and loses it again, and only
    /// then becomes a child of the list.
    /// </summary>
    PropagateWindowThenAddToList,
}
