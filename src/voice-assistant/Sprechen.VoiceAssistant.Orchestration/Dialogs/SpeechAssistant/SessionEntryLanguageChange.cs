using Microsoft.Extensions.Logging;
using Sprechen.Speech.LanguageContracts;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Übergibt eine am Einstieg angenommene Sprache dem Einstellungsablauf und bestimmt aus dessen
/// Rückgabe die Fortsetzungssprache. Eine zweite Bestätigungsfrage gehört nicht zu diesem
/// Speicherschritt. Die Annahme wurde im vorangegangenen Fragezug entschieden.
/// </summary>
internal sealed class SessionEntryLanguageChange(SessionEntryLanguageOfferDependencies dependencies)
{
    /// <summary>
    /// Speichert die angenommene Sprache über den gemeinsamen Einstellungsablauf. Eine verwendbare
    /// Rückgabe bestimmt die Fortsetzung. Fehlt sie, bleibt die bisherige Sprache erhalten.
    /// </summary>
    public async ValueTask<string> ApplyAcceptedLanguageAsync(
        string targetIsoCode,
        string selectedIsoCode,
        CancellationToken cancellationToken)
    {
        var settings = await dependencies.UserLanguageSettingsWorkflow
            .SaveSelectedLanguageAsync(targetIsoCode, cancellationToken)
            .ConfigureAwait(false);

        var persisted = IsoCode.TryNormalizeIso6391(settings.SelectedIsoCode);
        if (string.IsNullOrWhiteSpace(persisted))
        {
            dependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                "Language settings workflow returned no usable code for {IsoCode}; continuing in {SelectedIsoCode}.",
                targetIsoCode,
                selectedIsoCode);
            return selectedIsoCode;
        }

        // Hier keine Ansage auslösen: Der übergeordnete Ablauf ordnet Sprachrückmeldung und
        // anschließende Einführung an.
        dependencies.Logger.LogInformation(
            OrchestrationLogEvents.SessionEntryLanguageChanged,
            "Session-entry language decision changed the session language from {FromIsoCode} to {ToIsoCode}.",
            selectedIsoCode,
            persisted);
        return persisted;
    }
}
