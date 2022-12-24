using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace EightBot.Stellar.Maui;

public abstract class StyleManager : ReactiveObject
{
    protected Application CurrentApplication;

    private readonly ConcurrentDictionary<string, Style> _cachedStyles = new ConcurrentDictionary<string, Style>();

    public void ApplyStylesToApplication(Application app = null)
    {
        CurrentApplication = app ?? Application.Current;

        if (CurrentApplication.Resources == null)
        {
            CurrentApplication.Resources = new ResourceDictionary();
        }

        RegisterStyles(CurrentApplication);
    }

    protected abstract void RegisterStyles(Application app);

    protected Style GetStyle(Func<Style> styleCreator, [CallerMemberName] string name = null)
    {
        if (name == null)
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