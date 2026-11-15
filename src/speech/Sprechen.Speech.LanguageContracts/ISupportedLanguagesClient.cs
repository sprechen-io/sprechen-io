namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Lädt Sprachangebot und Standardsprache zur Bereitstellung des von der Anwendung verwendeten Sprachkatalogs.
/// </summary>
public interface ISupportedLanguagesClient
{
    Task<SupportedLanguages> GetAsync(CancellationToken ct);
}
