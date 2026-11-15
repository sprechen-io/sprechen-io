namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

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
    /// Ändert nur den Marker für das gehörte Sprachangebot und erhält die übrigen Einstellungen.
    /// Die getrennte Operation verhindert, dass der Aufrufer dafür einen möglicherweise fehlgeschlagenen Lesevorgang mit anschließendem vollständigem Überschreiben benötigt.
    /// </summary>
    public ValueTask SetEntryOfferHeardAsync(string isoCode, CancellationToken cancellationToken)
    {
        _value = _value with { EntryOfferHeardIsoCode = isoCode };
        return ValueTask.CompletedTask;
    }
}
