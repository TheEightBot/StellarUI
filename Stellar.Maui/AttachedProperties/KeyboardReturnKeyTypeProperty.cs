using System;
using System.Linq;

namespace Stellar.Maui.AttachedProperties;

public enum EntryKeyboardReturnType
{
    Default,
    Next,
    Go,
    Done,
    Send,
    Search,
}

public static class KeyboardReturnKeyTypeProperty
{
    public const string
        KeyboardReturnKeyTypeName = "KeyboardReturnKeyType",
        NextVisualElementName = "NextVisualElement";

    public static VisualElement GetNextVisualElement(BindableObject view)
    {
        return (VisualElement)view.GetValue(NextVisualElementProperty);
    }

    public static void SetNextVisualElement(BindableObject view, VisualElement value)
    {
        view.SetValue(NextVisualElementProperty, value);
    }

    public static BindableProperty NextVisualElementProperty =
        BindableProperty.CreateAttached(NextVisualElementName, typeof(VisualElement),
            typeof(Nullable), null, defaultBindingMode: BindingMode.Default,
            propertyChanged: OnNextVisualElementChanged);

    private static void OnNextVisualElementChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var ve = bindable as VisualElement;

        if (ve is null)
        {
            return;
        }

        var foundEffect = ve.Effects.FirstOrDefault(x => x.ResolveId == Effects.EffectNames.KeyboardReturnKeyTypeNameEffect);

        if (foundEffect != null)
        {
            ve.Effects.Remove(foundEffect);
        }

        var effect = Effect.Resolve(Effects.EffectNames.KeyboardReturnKeyTypeNameEffect);
        ve.Effects.Add(effect);
    }

    public static EntryKeyboardReturnType GetKeyboardReturnKeyType(BindableObject view)
    {
        return (EntryKeyboardReturnType)view.GetValue(EntryKeyboardReturnKeyTypeProperty);
    }

    public static void SetKeyboardReturnKeyType(BindableObject view, EntryKeyboardReturnType value)
    {
        view.SetValue(EntryKeyboardReturnKeyTypeProperty, value);
    }

    public static BindableProperty EntryKeyboardReturnKeyTypeProperty =
        BindableProperty.CreateAttached(KeyboardReturnKeyTypeName, typeof(EntryKeyboardReturnType),
            typeof(Nullable), EntryKeyboardReturnType.Default, defaultBindingMode: BindingMode.Default, propertyChanged: OnKeyboardReturnKeyTypeChanged);

    private static void OnKeyboardReturnKeyTypeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var ve = bindable as VisualElement;

        if (ve == null)
        {
            return;
        }

        var foundEffect = ve.Effects.FirstOrDefault(x => x.ResolveId == Effects.EffectNames.KeyboardReturnKeyTypeNameEffect);

        if (foundEffect != null)
        {
            ve.Effects.Remove(foundEffect);
        }

        var effect = Effect.Resolve(Effects.EffectNames.KeyboardReturnKeyTypeNameEffect);
        ve.Effects.Add(effect);
    }
}
