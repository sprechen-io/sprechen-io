namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Bereitet Sprache und Höflichkeitswahl für den Sitzungsstart vor und speichert die Auswahl.
/// </summary>
public interface ISessionSetupSettingsWorkflow
{
    IReadOnlyList<string> SupportedLanguages { get; }

    SessionSetupSettingsState GetSessionSetupState();

    SessionSetupSettingsState SelectSessionLanguage(string? selectedIsoCode, string? selectedPolitenessValue);

    ValueTask<SessionSetupSettingsState> SaveSessionSetupAsync(
        string? selectedIsoCode,
        string? selectedPolitenessValue,
        CancellationToken cancellationToken);
}
