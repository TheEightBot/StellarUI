using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Stellar.Maui;

public abstract class StyleManager : ReactiveObject
{
    private readonly ConcurrentDictionary<string, Style> _cachedStyles = new ConcurrentDictionary<string, Style>();

    public void ApplyStylesToApplication(Application? app = null)
    {
        var currentApplication = app ?? Application.Current;

        if (currentApplication is null)
        {
            return;
        }

        if (currentApplication!.Resources is null)
        {
            currentApplication.Resources = new ResourceDictionary();
        }

        RegisterStyles(currentApplication);
    }

    protected abstract void RegisterStyles(Application app);

    protected Style? GetStyle(Func<Style> styleCreator, [CallerMemberName] string? name = null)
    {
        if (name is null)
        {
            return default;
        }

        if (!_cachedStyles.ContainsKey(name))
        {
            _cachedStyles[name] = styleCreator.Invoke();
        }

        return _cachedStyles[name];
    }
}
