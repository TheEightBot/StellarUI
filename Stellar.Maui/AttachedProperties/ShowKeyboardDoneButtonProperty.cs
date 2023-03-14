using System;
using System.Linq;

namespace Stellar.Maui.AttachedProperties;

public class ShowKeyboardDoneButtonProperty
{
    public static BindableProperty KeyboardDoneButtonProperty =
        BindableProperty
            .Create(
                nameof(KeyboardDoneButtonProperty), typeof(bool), typeof(ShowKeyboardDoneButtonProperty), default(bool),
                defaultBindingMode: BindingMode.Default, propertyChanged: OnKeyboardDoneButtonChanged);

    public static bool GetKeyboardReturnKeyType(BindableObject view)
    {
        return (bool)view.GetValue(KeyboardDoneButtonProperty);
    }

    public static void SetKeyboardReturnKeyType(BindableObject view, bool value)
    {
        view.SetValue(KeyboardDoneButtonProperty, value);
    }

    private static void OnKeyboardDoneButtonChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var ve = bindable as VisualElement;

        if (ve is null)
        {
            return;
        }

        var shouldApply = (bool)newValue;

        var foundEffect = ve.Effects.FirstOrDefault(x => x.ResolveId == Effects.EffectNames.ShowKeyboardDoneButtonEffect);

        if (foundEffect is not null)
        {
            ve.Effects.Remove(foundEffect);
        }

        if (shouldApply)
        {
            var effect = Effect.Resolve(Effects.EffectNames.ShowKeyboardDoneButtonEffect);
            ve.Effects.Add(effect);
        }
    }
}
