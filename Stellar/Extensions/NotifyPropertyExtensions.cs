using System.Collections.Specialized;
using System.ComponentModel;

namespace Stellar;

public static class NotifyPropertyExtensions
{
    public static IObservable<PropertyChangedEventArgs> ObservePropertyChanged(this INotifyPropertyChanged notify, IScheduler? scheduler = null)
    {
        return
            Observable
                .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, PropertyChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => notify.PropertyChanged += x,
                    x => notify.PropertyChanged -= x)
                .ObserveOnIfNotNull(scheduler);
    }

    public static IObservable<PropertyChangingEventArgs> ObservePropertyChanging(this INotifyPropertyChanging notify, IScheduler? scheduler = null)
    {
        return
            Observable
                .FromEvent<PropertyChangingEventHandler, PropertyChangingEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, PropertyChangingEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => notify.PropertyChanging += x,
                    x => notify.PropertyChanging -= x)
                .ObserveOnIfNotNull(scheduler);
    }

    public static IObservable<NotifyCollectionChangedEventArgs> ObserveCollectionChanged(this INotifyCollectionChanged notify, IScheduler? scheduler = null)
    {
        return
            Observable
                .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, NotifyCollectionChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => notify.CollectionChanged += x,
                    x => notify.CollectionChanged -= x)
                .ObserveOnIfNotNull(scheduler);
    }

    private static IObservable<T> ObserveOnIfNotNull<T>(this IObservable<T> source, IScheduler? scheduler)
    {
        return scheduler is not null ? source.ObserveOn(scheduler) : source;
    }
}
