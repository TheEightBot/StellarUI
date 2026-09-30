using System.Reflection;
using System.Windows.Input;

namespace Stellar.Maui;

/// <summary>
/// Updates a control's <c>Command</c> in place when the command observed by a
/// <c>BindCommand</c> binding changes, which happens whenever the view's view model is
/// replaced: MAUI reuses a CollectionView cell by giving the same view a new
/// BindingContext. Without this, ReactiveUI 24 creates the replacement binding before
/// it disposes the previous one, and disposing the previous one restores the command
/// the control had before it was bound, so the control is left with no command.
/// </summary>
public sealed class MauiCommandRebinding : ICreatesCustomizedCommandRebinding
{
    public bool TryUpdateCommand<TControl>(TControl? control, ICommand? command)
        where TControl : class
    {
        if (control is null || CommandProperty<TControl>.Value is not { } commandProperty)
        {
            return false;
        }

        commandProperty.SetValue(control, command);

        return true;
    }

    // ReactiveUI binds through the Command property only when the static control type has
    // public Command and CommandParameter properties, so the same test decides whether
    // there is a property binding to update.
    private static class CommandProperty<TControl>
    {
        internal static readonly PropertyInfo? Value = Resolve(typeof(TControl));

        private static PropertyInfo? Resolve(Type type)
        {
            PropertyInfo? command = null;
            var hasCommandParameter = false;

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.Name == "Command")
                {
                    command ??= property;
                }
                else if (property.Name == "CommandParameter")
                {
                    hasCommandParameter = true;
                }
            }

            return hasCommandParameter ? command : null;
        }
    }
}
