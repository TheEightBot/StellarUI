namespace ReactiveUI.Maui;

/// <summary>
/// This is an <see cref="Frame"/> that is also an <see cref="IViewFor{T}"/>.
/// </summary>
/// <typeparam name="TViewModel">The type of the view model.</typeparam>
/// <seealso cref="Microsoft.Maui.Controls.Frame" />
/// <seealso cref="ReactiveUI.IViewFor{TViewModel}" />
public class ReactiveFrame<TViewModel> : Frame, IViewFor<TViewModel>
    where TViewModel : class
{
    /// <summary>
    /// The view model bindable property.
    /// </summary>
    public static readonly BindableProperty ViewModelProperty = BindableProperty.Create(
        nameof(ViewModel),
        typeof(TViewModel),
        typeof(ReactiveFrame<TViewModel>),
        default(TViewModel),
        BindingMode.OneWay,
        propertyChanged: OnViewModelChanged);

    /// <summary>
    /// Gets or sets the ViewModel to display.
    /// </summary>
    public TViewModel ViewModel
    {
        get => (TViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <inheritdoc/>
    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (TViewModel)value;
    }

    /// <inheritdoc/>
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        ViewModel = BindingContext as TViewModel;
    }

    private static void OnViewModelChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        bindableObject.BindingContext = newValue;
    }
}