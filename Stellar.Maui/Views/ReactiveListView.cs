using System.Collections.Concurrent;

namespace Stellar.Maui.Views;

public class ReactiveListView : ActivatableListView
{
    public ReactiveListView(Type cellType, ListViewCachingStrategy cachingStrategy = ListViewCachingStrategy.RecycleElement)
        : this(cachingStrategy)
    {
        ItemTemplate = new DataTemplate(cellType);
    }

    public ReactiveListView(Func<object> loadTemplate, ListViewCachingStrategy cachingStrategy = ListViewCachingStrategy.RecycleElement)
        : this(cachingStrategy)
    {
        ItemTemplate = new DataTemplate(loadTemplate);
    }

    public ReactiveListView(ListViewCachingStrategy cachingStrategy = ListViewCachingStrategy.RecycleElement)
        : base(cachingStrategy)
    {
    }
}
