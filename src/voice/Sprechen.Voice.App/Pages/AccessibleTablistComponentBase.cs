using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Sprechen.Voice.App.Pages;

/// <summary>
/// Stellt die Tastatursteuerung und Fokussierung für Tablisten bereit,
/// damit Einstellungsseiten dieselben Navigationstasten verwenden.
/// </summary>
public abstract class AccessibleTablistComponentBase : ComponentBase
{
    private ElementReference[] _tabReferences = [];

    protected abstract int TabCount { get; }

    protected abstract int ActiveTabIndex { get; }

    protected abstract void ActivateTab(int index);

    protected ElementReference[] TabReferences
    {
        get
        {
            if (_tabReferences.Length != TabCount)
            {
                _tabReferences = new ElementReference[TabCount];
            }

            return _tabReferences;
        }
    }

    protected async Task OnTablistKeyDownAsync(KeyboardEventArgs args)
    {
        var count = TabCount;
        if (count == 0)
        {
            return;
        }

        var current = ActiveTabIndex;
        int? target = args.Key switch
        {
            "ArrowRight" => (current + 1) % count,
            "ArrowLeft" => (current - 1 + count) % count,
            "Home" => 0,
            "End" => count - 1,
            _ => null,
        };

        if (target is not int index || index == current)
        {
            return;
        }

        ActivateTab(index);
        await FocusTabAsync(index);
    }

    private async Task FocusTabAsync(int index)
    {
        var references = TabReferences;
        if (index < 0 || index >= references.Length)
        {
            return;
        }

        try
        {
            await references[index].FocusAsync();
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
