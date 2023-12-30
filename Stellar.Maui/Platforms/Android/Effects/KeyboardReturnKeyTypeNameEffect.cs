using System;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Microsoft.Maui.Controls;
using Stellar.Maui.AttachedProperties;
using Stellar.Maui.Effects;
using Stellar.Maui.Platforms.Android.Effects;

[assembly: ExportEffect(typeof(KeyboardReturnKeyTypeNameEffect), nameof(KeyboardReturnKeyTypeNameEffect))]

namespace Stellar.Maui.Platforms.Android.Effects;

public class KeyboardReturnKeyTypeNameEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
{
    private ImeAction _startingAction;
    private string? _startingImeActionLabel;

    public static string EffectName => EffectNames.KeyboardReturnKeyTypeNameEffect;

    protected override void OnAttached()
    {
        if (Control is not EditText editText)
        {
            return;
        }

        _startingAction = editText.ImeOptions;
        _startingImeActionLabel = editText.ImeActionLabel;

        editText.EditorAction -= EditText_EditorAction;
        editText.EditorAction += EditText_EditorAction;

        SetReturnType();
    }

    protected override void OnDetached()
    {
        if (Control is not EditText editText || editText.Handle == IntPtr.Zero)
        {
            return;
        }

        editText.EditorAction -= EditText_EditorAction;

        editText.ImeOptions = _startingAction;
        editText.SetImeActionLabel(_startingImeActionLabel, _startingAction);
    }

    protected override void OnElementPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args?.PropertyName?.Equals(KeyboardReturnKeyTypeProperty.KeyboardReturnKeyTypeName) == true)
        {
            SetReturnType();
            return;
        }

        base.OnElementPropertyChanged(args);
    }

    private void SetReturnType()
    {
        if (!(Control is EditText editText))
        {
            return;
        }

        switch (KeyboardReturnKeyTypeProperty.GetKeyboardReturnKeyType(Element))
        {
            case EntryKeyboardReturnType.Go:
                editText.ImeOptions = ImeAction.Go;
                editText.SetImeActionLabel("Go", ImeAction.Go);
                break;
            case EntryKeyboardReturnType.Next:
                editText.ImeOptions = ImeAction.Next;
                editText.SetImeActionLabel("Next", ImeAction.Next);
                break;
            case EntryKeyboardReturnType.Send:
                editText.ImeOptions = ImeAction.Send;
                editText.SetImeActionLabel("Send", ImeAction.Send);
                break;
            case EntryKeyboardReturnType.Search:
                editText.ImeOptions = ImeAction.Search;
                editText.SetImeActionLabel("Search", ImeAction.Search);
                break;
            case EntryKeyboardReturnType.Default:
                editText.ImeOptions = _startingAction;
                editText.SetImeActionLabel(_startingImeActionLabel, _startingAction);
                break;
        }
    }

    private void EditText_EditorAction(object? sender, TextView.EditorActionEventArgs e)
    {
        if (e.ActionId.Equals(ImeAction.Next))
        {
            var nextElement = KeyboardReturnKeyTypeProperty.GetNextVisualElement(Element);

            if (nextElement is not null)
            {
                nextElement.Focus();
            }
        }
    }
}
