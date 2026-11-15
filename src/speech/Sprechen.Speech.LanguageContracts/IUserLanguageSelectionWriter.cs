namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Speichert eine Sprachwahl und liefert die resultierenden Einstellungen zur weiteren Verarbeitung im Einstellungsablauf.
/// </summary>
public interface IUserLanguageSelectionWriter
{
    ValueTask<UserLanguageSettings> SaveSelectedLanguageAsync(
        string isoCode,
        CancellationToken cancellationToken);
}
