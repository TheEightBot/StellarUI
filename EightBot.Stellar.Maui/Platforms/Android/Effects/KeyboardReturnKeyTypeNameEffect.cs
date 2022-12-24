using System;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using EightBot.Stellar.Maui.AttachedProperties;
using EightBot.Stellar.Maui.Effects;
using EightBot.Stellar.Maui.Platforms.Android.Effects;
using Microsoft.Maui.Controls;

[assembly: ExportEffect(typeof(KeyboardReturnKeyTypeNameEffect), nameof(KeyboardReturnKeyTypeNameEffect))]

namespace EightBot.Stellar.Maui.Platforms.Android.Effects;

public class KeyboardReturnKeyTypeNameEffect : Microsoft.Maui.Controls.Platform.PlatformEffect
{
    private ImeAction _startingAction;
    private string _startingImeActionLabel;

    public static string EffectName => EffectNames.KeyboardReturnKeyTypeNameEffect;

    protected override void OnAttached()
    {
        var editText = Control as EditText;

        if (editText == null)
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
        var editText = Control as EditText;

        if (editText == null || editText.Handle == IntPtr.Zero)
        {
            return;
        }

        editText.EditorAction -= EditText_EditorAction;

        editText.ImeOptions = _startingAction;
        editText.SetImeActionLabel(_startingImeActionLabel, _startingAction);
    }

    protected override void OnElementPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName.Equals(KeyboardReturnKeyTypeProperty.KeyboardReturnKeyTypeName))
        {
            SetReturnType();
            return;
        }

        base.OnElementPropertyChanged(args);
    }

    private void SetReturnType()
    {
        var editText = Control as EditText;

        if (editText == null)
        {
            return;
        }

        var keyboardType = KeyboardReturnKeyTypeProperty.GetKeyboardReturnKeyType(Element);

        switch (keyboardType)
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

    private void EditText_EditorAction(object sender, TextView.EditorActionEventArgs e)
    {
        if (e.ActionId.Equals(ImeAction.Next))
        {
            var nextElement = KeyboardReturnKeyTypeProperty.GetNextVisualElement(Element);

            if (nextElement != null)
            {
                nextElement.Focus();
            }
        }
    }
}