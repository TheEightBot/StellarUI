using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// Wraps INotifyPropertyChanged / INotifyPropertyChanging / INotifyCollectionChanged as
/// observables. The handler plumbing is easy to get subtly wrong, particularly detaching
/// on unsubscribe, which is what leaks if it regresses.
/// </summary>
public class NotifyPropertyExtensionsTests
{
    [Fact]
    public void ObservePropertyChanged_EmitsTheChangedProperty()
    {
        var subject = new Notifier();
        var seen = new List<string?>();

        using var subscription = subject.ObservePropertyChanged().Subscribe(e => seen.Add(e.PropertyName));

        subject.Value = "one";
        subject.Value = "two";

        Assert.Equal(new[] { nameof(Notifier.Value), nameof(Notifier.Value) }, seen);
    }

    [Fact]
    public void ObservePropertyChanged_DetachesOnUnsubscribe()
    {
        var subject = new Notifier();
        var seen = 0;

        var subscription = subject.ObservePropertyChanged().Subscribe(_ => seen++);
        subject.Value = "before";
        subscription.Dispose();
        subject.Value = "after";

        Assert.Equal(1, seen);
        Assert.False(subject.HasPropertyChangedSubscribers);
    }

    [Fact]
    public void ObservePropertyChanged_HonoursTheSuppliedScheduler()
    {
        var subject = new Notifier();
        var scheduler = new Microsoft.Reactive.Testing.TestScheduler();
        var seen = 0;

        using var subscription = subject.ObservePropertyChanged(scheduler).Subscribe(_ => seen++);

        subject.Value = "one";
        Assert.Equal(0, seen);

        scheduler.Start();
        Assert.Equal(1, seen);
    }

    [Fact]
    public void ObservePropertyChanging_EmitsBeforeTheChange()
    {
        var subject = new Notifier();
        var valuesAtNotification = new List<string?>();

        using var subscription = subject
            .ObservePropertyChanging()
            .Subscribe(_ => valuesAtNotification.Add(subject.Value));

        subject.Value = "updated";

        // Changing fires before the field is written, so the old value is still visible.
        Assert.Equal(new string?[] { null }, valuesAtNotification);
    }

    [Fact]
    public void ObservePropertyChanging_DetachesOnUnsubscribe()
    {
        var subject = new Notifier();
        var seen = 0;

        var subscription = subject.ObservePropertyChanging().Subscribe(_ => seen++);
        subject.Value = "before";
        subscription.Dispose();
        subject.Value = "after";

        Assert.Equal(1, seen);
        Assert.False(subject.HasPropertyChangingSubscribers);
    }

    [Fact]
    public void ObserveCollectionChanged_EmitsTheChangeAction()
    {
        var collection = new ObservableCollection<int>();
        var actions = new List<NotifyCollectionChangedAction>();

        using var subscription = collection.ObserveCollectionChanged().Subscribe(e => actions.Add(e.Action));

        collection.Add(1);
        collection.Remove(1);
        collection.Add(2);
        collection.Clear();

        Assert.Equal(
            new[]
            {
                NotifyCollectionChangedAction.Add,
                NotifyCollectionChangedAction.Remove,
                NotifyCollectionChangedAction.Add,
                NotifyCollectionChangedAction.Reset,
            },
            actions);
    }

    [Fact]
    public void ObserveCollectionChanged_DetachesOnUnsubscribe()
    {
        var collection = new ObservableCollection<int>();
        var seen = 0;

        var subscription = collection.ObserveCollectionChanged().Subscribe(_ => seen++);
        collection.Add(1);
        subscription.Dispose();
        collection.Add(2);

        Assert.Equal(1, seen);
    }

    [Fact]
    public void ObserveCollectionChanged_HonoursTheSuppliedScheduler()
    {
        var collection = new ObservableCollection<int>();
        var scheduler = new Microsoft.Reactive.Testing.TestScheduler();
        var seen = 0;

        using var subscription = collection.ObserveCollectionChanged(scheduler).Subscribe(_ => seen++);

        collection.Add(1);
        Assert.Equal(0, seen);

        scheduler.Start();
        Assert.Equal(1, seen);
    }

    [Fact]
    public void ObservePropertyChanging_HonoursTheSuppliedScheduler()
    {
        var subject = new Notifier();
        var scheduler = new Microsoft.Reactive.Testing.TestScheduler();
        var seen = 0;

        using var subscription = subject.ObservePropertyChanging(scheduler).Subscribe(_ => seen++);

        subject.Value = "one";
        Assert.Equal(0, seen);

        scheduler.Start();
        Assert.Equal(1, seen);
    }

    private sealed class Notifier : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private string? _value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event PropertyChangingEventHandler? PropertyChanging;

        public bool HasPropertyChangedSubscribers => PropertyChanged is not null;

        public bool HasPropertyChangingSubscribers => PropertyChanging is not null;

        public string? Value
        {
            get => _value;
            set
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Value)));
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
