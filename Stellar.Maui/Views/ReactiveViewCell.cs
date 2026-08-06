// StellarUI continues to support ListView for as long as .NET MAUI ships it. MAUI marks
// ListView and its cell types obsolete in favour of CollectionView, but removing this
// surface would break every consumer still using it, so the deprecation is suppressed here
// rather than propagated. Revisit when MAUI actually removes the types.
#pragma warning disable CS0618 // Type or member is obsolete

// ReactiveUI.Maui shipped a ReactiveViewCell up to version 23; ReactiveUI 24 dropped it,
// so Stellar provides its own. It lives in this namespace alongside ReactiveGrid and
// ReactiveStackLayout, which Stellar has always supplied itself.
namespace ReactiveUI.Maui.Views;

/// <summary>
/// This is a <see cref="ViewCell"/> that is also an <see cref="IViewFor{T}"/>.
/// </summary>
/// <typeparam name="TViewModel">The type of the view model.</typeparam>
/// <seealso cref="Microsoft.Maui.Controls.ViewCell" />
/// <seealso cref="ReactiveUI.IViewFor{TViewModel}" />
public class ReactiveViewCell<TViewModel> : ViewCell, IViewFor<TViewModel>
    where TViewModel : class
{
    /// <summary>
    /// The view model bindable property.
    /// </summary>
    public static readonly BindableProperty ViewModelProperty = BindableProperty.Create(
        nameof(ViewModel),
        typeof(TViewModel),
        typeof(ReactiveViewCell<TViewModel>),
        default(TViewModel),
        BindingMode.OneWay,
        propertyChanged: OnViewModelChanged);

    /// <summary>
    /// Gets or sets the ViewModel to display.
    /// </summary>
    public TViewModel? ViewModel
    {
        get => (TViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <inheritdoc/>
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as TViewModel;
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
