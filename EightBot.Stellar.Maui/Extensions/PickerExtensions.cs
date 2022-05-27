using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq.Expressions;
using ReactiveUI;

namespace EightBot.Stellar.Maui;

public static class PickerExtensions
{
    public static IDisposable Bind<TViewIn, TItemsModel>(
        this TViewIn view,
        Expression<Func<TViewIn, Picker>> selectPicker,
        IEnumerable<TItemsModel> items,
        Action<TItemsModel> selectedItemChanged,
        Func<TItemsModel, bool> selectItem,
        Func<TItemsModel, string> titleSelector)
        where TViewIn : VisualElement
    {
        var binderDisposable = new SerialDisposable();

        var pickerSubscription =
            view.WhenAnyValue(selectPicker)
                .IsNotNull()
                .Select(picker => binderDisposable.Disposable = new ReactivePickerBinder<TItemsModel>(picker, items, selectedItemChanged, selectItem, titleSelector, null))
                .Subscribe();

        return Disposable
            .Create(
            () =>
            {
                pickerSubscription?.Dispose();
                binderDisposable?.Dispose();
            });
    }

    public static ReactivePickerBinder<TViewModel> Bind<TViewModel>(this Picker picker, IEnumerable<TViewModel> items, Action<TViewModel> selectedItemChanged, Func<TViewModel, bool> selectItem, Func<TViewModel, string> titleSelector)
    {
        return new ReactivePickerBinder<TViewModel>(picker, items, selectedItemChanged, selectItem, titleSelector, null);
    }

    public static IDisposable Bind<TViewIn, TItemsModel, TDontCare>(
        this TViewIn view,
        Expression<Func<TViewIn, Picker>> selectPicker,
        IEnumerable<TItemsModel> items,
        Action<TItemsModel> selectedItemChanged,
        Func<TItemsModel, bool> selectItem,
        Func<TItemsModel, string> titleSelector,
        IObservable<TDontCare> signalRefresh = null)
        where TViewIn : VisualElement
    {
        IObservable<Unit> refresh = null;
        if (signalRefresh != null)
        {
            refresh = signalRefresh.Select(_ => Unit.Default);
        }

        var binderDisposable = new SerialDisposable();

        var pickerSubscription =
            view.WhenAnyValue(selectPicker)
                .IsNotNull()
                .DistinctUntilChanged()
                .Select(picker => binderDisposable.Disposable = new ReactivePickerBinder<TItemsModel>(picker, items, selectedItemChanged, selectItem, titleSelector, refresh))
                .Subscribe();

        return Disposable
            .Create(
            () =>
            {
                pickerSubscription?.Dispose();
                binderDisposable?.Dispose();
            });
    }

    public static ReactivePickerBinder<TViewModel> Bind<TViewModel, TDontCare>(
        this Picker picker,
        IEnumerable<TViewModel> items,
        Action<TViewModel> selectedItemChanged,
        Func<TViewModel, bool> selectItem,
        Func<TViewModel, string> titleSelector,
        IObservable<TDontCare> signalRefresh = null)
    {
        IObservable<Unit> refresh = null;
        if (signalRefresh != null)
        {
            refresh = signalRefresh.Select(_ => Unit.Default);
        }

        return new ReactivePickerBinder<TViewModel>(picker, items, selectedItemChanged, selectItem, titleSelector, refresh);
    }

    public static IDisposable Bind<TViewIn, TItemsModel>(
        this TViewIn view,
        Expression<Func<TViewIn, Picker>> selectPicker,
        IObservable<IEnumerable<TItemsModel>> items,
        Action<TItemsModel> selectedItemChanged,
        Func<TItemsModel, bool> selectItem,
        Func<TItemsModel, string> titleSelector)
        where TViewIn : VisualElement
    {
        var binderDisposable = new SerialDisposable();

        var pickerSubscription =
            view.WhenAnyValue(selectPicker)
                .IsNotNull()
                .DistinctUntilChanged()
                .Select(picker => binderDisposable.Disposable = new ReactivePickerBinder<TItemsModel>(picker, items, selectedItemChanged, selectItem, titleSelector, null))
                .Subscribe();

        return Disposable
            .Create(
            () =>
            {
                pickerSubscription?.Dispose();
                binderDisposable?.Dispose();
            });
    }

    public static ReactivePickerBinder<TViewModel> Bind<TViewModel>(this Picker picker, IObservable<IEnumerable<TViewModel>> items, Action<TViewModel> selectedItemChanged, Func<TViewModel, bool> selectItem, Func<TViewModel, string> titleSelector)
    {
        return new ReactivePickerBinder<TViewModel>(picker, items, selectedItemChanged, selectItem, titleSelector, null);
    }

    public static IDisposable BindPicker<TViewIn, TViewModel, TItemsModel, TDontCare>(
        this TViewIn view,
        Expression<Func<TViewIn, Picker>> selectPicker,
        IObservable<IEnumerable<TItemsModel>> items,
        Action<TItemsModel> selectedItemChanged,
        Func<TItemsModel, bool> selectItem,
        Func<TItemsModel, string> titleSelector,
        IObservable<TDontCare> signalRefresh = null)
        where TViewIn : IViewFor<TViewModel>
        where TViewModel : class
    {
        IObservable<Unit> refresh = null;
        if (signalRefresh != null)
        {
            refresh = signalRefresh.Select(_ => Unit.Default);
        }

        var binderDisposable = new SerialDisposable();

        var pickerSubscription =
            view.WhenAnyValue(selectPicker)
                .IsNotNull()
                .DistinctUntilChanged()
                .Select(picker => binderDisposable.Disposable = new ReactivePickerBinder<TItemsModel>(picker, items, selectedItemChanged, selectItem, titleSelector, refresh))
                .Subscribe();

        return Disposable
            .Create(
            () =>
            {
                pickerSubscription?.Dispose();
                binderDisposable?.Dispose();
            });
    }

    public static ReactivePickerBinder<TViewModel> Bind<TViewModel, TDontCare>(
        this Picker picker,
        IObservable<IEnumerable<TViewModel>> items,
        Action<TViewModel> selectedItemChanged,
        Func<TViewModel, bool> selectItem,
        Func<TViewModel, string> titleSelector,
        IObservable<TDontCare> signalRefresh = null)
    {
        IObservable<Unit> refresh = null;
        if (signalRefresh != null)
        {
            refresh = signalRefresh.Select(_ => Unit.Default);
        }

        return new ReactivePickerBinder<TViewModel>(picker, items, selectedItemChanged, selectItem, titleSelector, refresh);
    }

    public static void ResetToInitialValue(this Picker picker)
    {
        picker.SelectedItem = null;
        picker.SelectedIndex = -1;
    }
}

public class ReactivePickerBinder<TViewModel> : IDisposable
{
    private const string BindingContextPath = ".";

    private readonly Action<TViewModel> _selectedItemChanged;

    private readonly Func<TViewModel, bool> _selectItem;

    private readonly Picker _picker;

    private readonly CompositeDisposable _disposableSubscriptions;

    // Properties
    public TViewModel SelectedItem
        => _picker?.SelectedItem != null
            ? (TViewModel)_picker.SelectedItem
            : default(TViewModel);

    private (bool Success, TViewModel FoundItem) GetItemAt(int index)
        => index < (_picker?.ItemsSource?.Count ?? 0)
            ? (true, (TViewModel)_picker.ItemsSource[index])
            : (false, default(TViewModel));

    public ReactivePickerBinder(
        Picker picker,
        IEnumerable<TViewModel> items,
        Action<TViewModel> selectedItemChanged, Func<TViewModel, bool> selectItem, Func<TViewModel, string> titleSelector,
        IObservable<Unit> signalViewUpdate = null)
        : this(picker, Observable.Return(items),
            selectedItemChanged, selectItem, titleSelector,
            signalViewUpdate)
    {
    }

    public ReactivePickerBinder(
        Picker picker,
        IObservable<IEnumerable<TViewModel>> items,
        Action<TViewModel> selectedItemChanged, Func<TViewModel, bool> selectItem, Func<TViewModel, string> titleSelector,
        IObservable<Unit> signalViewUpdate = null)
    {
        _picker = picker;

        _picker.ItemDisplayBinding = new Binding(BindingContextPath, BindingMode.OneTime, new TitleSelectorConverter<TViewModel>(titleSelector));

        _selectedItemChanged = selectedItemChanged;
        _selectItem = selectItem;

        _disposableSubscriptions = new CompositeDisposable();

        Observable
            .CombineLatest(
                _picker
                    .WhenAnyValue(x => x.Window),
                items
                    .Select(itms => itms is IList il ? il : itms?.ToList()),
                (window, items) => (HasWindow: window != null, Items: items))
            .Where(x => x.HasWindow)
            .Select(x => x.Items)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(items => _picker.ItemsSource = items)
            .Subscribe()
            .DisposeWith(_disposableSubscriptions);

        _picker
            .WhenAnyValue(x => x.ItemsSource)
            .IsNotNull()
            .Select(_ => signalViewUpdate?.Select(_ => true) ?? Observable.Return(false))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(fromNotificationTrigger => SetSelectedItem(fromNotificationTrigger))
            .Subscribe()
            .DisposeWith(_disposableSubscriptions);

        _picker
            .WhenAnyValue(x => x.SelectedItem)
            .Select(item => item != null ? (TViewModel)item : default(TViewModel))
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(item => SelectedItemChanged(item, true))
            .Subscribe()
            .DisposeWith(_disposableSubscriptions);

        Observable
            .FromEvent<EventHandler<FocusEventArgs>, FocusEventArgs>(
                eventHandler =>
                {
                    void Handler(object sender, FocusEventArgs e) => eventHandler?.Invoke(e);
                    return Handler;
                },
                x => _picker.Focused += x,
                x =>
                {
                    if (_picker != null)
                    {
                        _picker.Focused -= x;
                    }
                })
            .Where(_ => DeviceInfo.Platform == DevicePlatform.iOS)
            .Where(args => args.IsFocused)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(
                _ =>
                {
                    if (_picker != null && _picker.SelectedIndex < 0 && _picker.Items.Count > 0)
                    {
                        _picker.SelectedIndex = 0;
                    }
                })
            .DisposeWith(_disposableSubscriptions);
    }

    private void SetSelectedItem(bool fromNotificationTrigger = false)
    {
        var pickerItemCount = _picker?.ItemsSource?.Count ?? 0;
        if (pickerItemCount <= 0)
        {
            return;
        }

        for (int index = 0; index < pickerItemCount; index++)
        {
            var itemAt = this.GetItemAt(index);
            if (itemAt.Success && _selectItem.Invoke(itemAt.FoundItem))
            {
                if ((itemAt.FoundItem?.Equals(default(TViewModel)) ?? false) || !EqualityComparer<TViewModel>.Default.Equals(itemAt.FoundItem, this.SelectedItem))
                {
                    SelectedItemChanged(itemAt.FoundItem);
                }

                return;
            }
        }

        if (fromNotificationTrigger)
        {
            SelectedItemChanged(default(TViewModel));
        }
    }

    private void SelectedItemChanged(TViewModel item, bool fromUi = false)
    {
        _selectedItemChanged?.Invoke(item);

        if (!fromUi)
        {
            if (_picker != null && (_picker.SelectedItem == null || !EqualityComparer<TViewModel>.Default.Equals(item, this.SelectedItem)))
            {
               _picker.SelectedItem = item;
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposableSubscriptions?.Dispose();
        }
    }
}

internal class TitleSelectorConverter<TViewModel> : IValueConverter
{
    private readonly Func<TViewModel, string> _titleSelector;

    public TitleSelectorConverter(Func<TViewModel, string> titleSelector)
    {
        _titleSelector = titleSelector;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return
            value is TViewModel viewModel
                ? _titleSelector.Invoke(viewModel)
                : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}