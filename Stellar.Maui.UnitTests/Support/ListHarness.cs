using Microsoft.Maui.Controls.Internals;

namespace Stellar.Maui.UnitTests.Support;

/// <summary>
/// A CollectionView in a window, driven the way the MAUI handlers drive it. No handler or
/// platform view exists here, so each method makes the same calls on the cross-platform
/// elements that the platform code makes.
/// </summary>
internal sealed class ListHarness
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListHarness"/> class.
    /// </summary>
    /// <param name="recycledItemViewLimit">
    /// The limit the list opts in with, or null to leave the list as an app that never
    /// calls RecycledItemViewLimit would have it.
    /// </param>
    public ListHarness(int? recycledItemViewLimit = 5)
    {
        if (recycledItemViewLimit is { } limit)
        {
            List.RecycledItemViewLimit(limit);
        }

        Page = new ContentPage { Content = new Grid { Children = { List, Side } } };
        Window = new Window(Page);
    }

    public CollectionView List { get; } = new();

    public VerticalStackLayout Side { get; } = new();

    public ContentPage Page { get; }

    public Window Window { get; }

    /// <summary>
    /// Binds a view that has just come out of the template. On Android this is
    /// TemplatedItemViewHolder.Bind taking its template branch.
    /// </summary>
    public void BindNew(ITestCell cell, object row, FirstBind firstBind = FirstBind.AddToList)
    {
        var view = (View)cell;

        view.BindingContext = row;

        if (firstBind == FirstBind.PropagateWindowThenAddToList)
        {
            PropertyPropagationExtensions.PropagatePropertyChanged(null, view, List);
        }

        List.AddLogicalChild(view);
    }

    /// <summary>
    /// Binds a view the list has used before. On Android this is
    /// TemplatedItemViewHolder.Bind for a holder that already has its view.
    /// </summary>
    public void Bind(ITestCell cell, object row)
    {
        var view = (View)cell;

        view.BindingContext = row;
        List.AddLogicalChild(view);
    }

    /// <summary>
    /// Android's TemplatedItemViewHolder.Recycle.
    /// </summary>
    public void Recycle(ITestCell cell) => List.RemoveLogicalChild((View)cell);

    public void RemoveFromWindow() => Window.Page = new ContentPage();

    public void ReturnToWindow() => Window.Page = Page;
}
