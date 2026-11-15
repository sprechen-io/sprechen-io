using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Auflösung der Zielsprache, sprachbezogene Rückfragen und das Speichern
/// der Sprachauswahl. Der Katalogdialog verwendet damit dieselben Sprachbezeichnungen
/// und Benutzervorgaben wie andere Abläufe.
/// </summary>
internal sealed class FormsCatalogLanguageChangeWorkflow(
    IUserLanguageSelectionWriter userLanguageSelectionWriter,
    LanguageChangeTargetResolver languageChangeTargetResolver)
{
    private readonly IUserLanguageSelectionWriter _userLanguageSelectionWriter =
        userLanguageSelectionWriter ?? throw new ArgumentNullException(nameof(userLanguageSelectionWriter));
    private readonly LanguageChangeTargetResolver _languageChangeTargetResolver =
        languageChangeTargetResolver ?? throw new ArgumentNullException(nameof(languageChangeTargetResolver));

    public string FormatSupportedLanguages(
        IReadOnlyCollection<string> supportedLanguages,
        string isoCode)
    {
        ArgumentNullException.ThrowIfNull(supportedLanguages);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        return _languageChangeTargetResolver.FormatSupportedLanguages(supportedLanguages, isoCode);
    }

    public bool TryResolveTarget(
        string rawLanguageTarget,
        IReadOnlyCollection<string> supportedLanguages,
        string currentIsoCode,
        out string targetIsoCode,
        out string targetDisplayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawLanguageTarget);
        ArgumentNullException.ThrowIfNull(supportedLanguages);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentIsoCode);

        if (!_languageChangeTargetResolver.TryResolve(
                rawLanguageTarget,
                currentIsoCode,
                out targetIsoCode,
                out targetDisplayName))
        {
            return false;
        }

        return supportedLanguages.Contains(targetIsoCode, StringComparer.OrdinalIgnoreCase);
    }

    public string GetConfirmationPrompt(
        string isoCode,
        string targetDisplayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDisplayName);

        return FormDialogFlowPromptTextResolver.GetConfirmationPrompt(isoCode, targetDisplayName);
    }

    /// <summary>
    /// Liefert die Eigenbezeichnung der Zielsprache für die dort gesprochene Bestätigungsfrage.
    /// Der Sprachname hängt dadurch nicht vom Verständnis der bisherigen Sitzungssprache ab.
    /// </summary>
    public string GetEndonym(string targetIsoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetIsoCode);

        return _languageChangeTargetResolver.GetDisplayName(targetIsoCode, targetIsoCode);
    }

    public string GetLanguageKeptPrompt(string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var currentDisplayName = _languageChangeTargetResolver.GetDisplayName(isoCode, isoCode);
        return FormDialogFlowPromptTextResolver.GetKeptPrompt(isoCode, currentDisplayName);
    }

    public ValueTask<UserLanguageSettings> SaveSelectedLanguageAsync(
        string targetIsoCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetIsoCode);
        return _userLanguageSelectionWriter.SaveSelectedLanguageAsync(targetIsoCode, cancellationToken);
    }
}
