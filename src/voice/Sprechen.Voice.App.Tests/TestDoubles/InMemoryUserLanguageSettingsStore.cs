namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemoryUserLanguageSettingsStore : IUserLanguageSettingsStore
{
    private UserLanguageSettings _value = new(null, null);

    public ValueTask<UserLanguageSettings> GetAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_value);
    }

    public ValueTask SetAsync(UserLanguageSettings settings, CancellationToken cancellationToken)
    {
        _value = settings;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Ändert nur die Sprache, in der das Einstiegsangebot bereits gehört wurde.
    /// Die übrigen Einstellungen bleiben erhalten, damit Tests versehentliches Überschreiben erkennen können.
    /// </summary>
    public ValueTask SetEntryOfferHeardAsync(string isoCode, CancellationToken cancellationToken)
    {
        _value = _value with { EntryOfferHeardIsoCode = isoCode };
        return ValueTask.CompletedTask;
    }
}
