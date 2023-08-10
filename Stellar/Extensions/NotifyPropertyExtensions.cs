using System.Collections.Specialized;
using System.ComponentModel;

namespace Stellar;

public static class NotifyPropertyExtensions
{
    public static IObservable<PropertyChangedEventArgs> ObservePropertyChanged(this INotifyPropertyChanged notify, IScheduler scheduler = null)
    {
        if (scheduler is not null)
        {
            return
                Observable
                    .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                        static eventHandler =>
                        {
                            void Handler(object sender, PropertyChangedEventArgs e) => eventHandler?.Invoke(e);
                            return Handler;
                        },
                        x => notify.PropertyChanged += x,
                        x => notify.PropertyChanged -= x,
                        scheduler);
        }

        return
            Observable
                .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object sender, PropertyChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => notify.PropertyChanged += x,
                    x => notify.PropertyChanged -= x);
    }

    public static IObservable<PropertyChangingEventArgs> ObservePropertyChanging(this INotifyPropertyChanging notify, IScheduler scheduler = null)
    {
        if (scheduler is not null)
        {
            return
                Observable
                    .FromEvent<PropertyChangingEventHandler, PropertyChangingEventArgs>(
                        static eventHandler =>
                        {
                            void Handler(object sender, PropertyChangingEventArgs e) => eventHandler?.Invoke(e);
                            return Handler;
                        },
                        x => notify.PropertyChanging += x,
                        x => notify.PropertyChanging -= x,
                        scheduler);
        }

        return
            Observable
                .FromEvent<PropertyChangingEventHandler, PropertyChangingEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object sender, PropertyChangingEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => notify.PropertyChanging += x,
                    x => notify.PropertyChanging -= x);
    }

    public static IObservable<NotifyCollectionChangedEventArgs> ObserveCollectinChanged(this INotifyCollectionChanged notify, IScheduler scheduler = null)
    {
        if (scheduler is not null)
        {
            return
                Observable
                    .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                        static eventHandler =>
                        {
                            void Handler(object sender, NotifyCollectionChangedEventArgs e) => eventHandler?.Invoke(e);
                            return Handler;
                        },
                        x => notify.CollectionChanged += x,
                        x => notify.CollectionChanged -= x,
                        scheduler);
        }

        return
            Observable
                .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object sender, NotifyCollectionChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => notify.CollectionChanged += x,
                    x => notify.CollectionChanged -= x);
    }
}