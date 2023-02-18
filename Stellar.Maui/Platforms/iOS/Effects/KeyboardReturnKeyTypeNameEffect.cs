using System;
using Microsoft.Maui.Controls.Platform;
using Stellar.Maui.AttachedProperties;
using Stellar.Maui.Effects;
using Stellar.Maui.Platforms.iOS.Effects;
using UIKit;

[assembly: ExportEffect(typeof(KeyboardReturnKeyTypeNameEffect), nameof(KeyboardReturnKeyTypeNameEffect))]

namespace Stellar.Maui.Platforms.iOS.Effects;

public class KeyboardReturnKeyTypeNameEffect : PlatformEffect
{
    private UIReturnKeyType _startingReturnKeyType;

    public static string EffectName => EffectNames.KeyboardReturnKeyTypeNameEffect;

    protected override void OnAttached()
    {
        var textField = this.Control as UITextField;

        if (textField == null)
        {
            return;
        }

        _startingReturnKeyType = textField.ReturnKeyType;

        textField.ShouldReturn += TextField_ShouldReturn;

        SetReturnType();
    }

    protected override void OnDetached()
    {
        var textField = this.Control as UITextField;

        if (textField == null)
        {
            return;
        }

        textField.ShouldReturn -= TextField_ShouldReturn;

        textField.ReturnKeyType = _startingReturnKeyType;
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
        var textField = this.Control as UITextField;

        if (textField == null)
        {
            return;
        }

        _startingReturnKeyType = textField.ReturnKeyType;

        var keyboardType = KeyboardReturnKeyTypeProperty.GetKeyboardReturnKeyType(Element);

        switch (keyboardType)
        {
            case EntryKeyboardReturnType.Go:
                textField.ReturnKeyType = UIReturnKeyType.Go;
                break;
            case EntryKeyboardReturnType.Next:
                textField.ReturnKeyType = UIReturnKeyType.Next;
                break;
            case EntryKeyboardReturnType.Send:
                textField.ReturnKeyType = UIReturnKeyType.Send;
                break;
            case EntryKeyboardReturnType.Search:
                textField.ReturnKeyType = UIReturnKeyType.Search;
                break;
            case EntryKeyboardReturnType.Done:
                textField.ReturnKeyType = UIReturnKeyType.Done;
                break;
            case EntryKeyboardReturnType.Default:
                textField.ReturnKeyType = UIReturnKeyType.Default;
                break;
        }
    }

    private bool TextField_ShouldReturn(UITextField textField)
    {
        if (textField.ReturnKeyType.Equals(UIReturnKeyType.Next))
        {
            var nextElement = KeyboardReturnKeyTypeProperty.GetNextVisualElement(Element);

            if (nextElement != null)
            {
                nextElement.Focus();
            }
        }

        return true;
    }
}