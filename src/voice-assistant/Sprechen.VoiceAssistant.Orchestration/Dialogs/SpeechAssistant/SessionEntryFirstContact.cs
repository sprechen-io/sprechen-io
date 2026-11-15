using Microsoft.Extensions.Logging;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;
using Sprechen.Speech.App.Contracts.Services;
namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Verwaltet den Sprachmerker für das Einstiegsangebot getrennt vom Fragenablauf. Ein nicht lesbarer
/// Merker führt zum längeren Erstkontaktfenster. Gewöhnliche Schreibfehler werden protokolliert. Der
/// Aufrufer entscheidet, nach welchem Ergebnis ein Merker geschrieben wird.
/// </summary>
internal sealed class SessionEntryFirstContact(SessionEntryLanguageOfferDependencies dependencies)
{
    /// <summary>
    /// Vergleicht die aktuelle Sprache mit dem zuletzt gespeicherten Angebotsmerker. Ein abweichender
    /// oder nicht lesbarer Merker gilt als Erstkontakt, damit ein Speicherfehler nicht das kürzere
    /// Antwortfenster erzwingt.
    /// </summary>
    public async ValueTask<bool> IsFirstContactAsync(string selectedIsoCode, CancellationToken cancellationToken)
    {
        try
        {
            var heardIn = await dependencies.UserLanguageSettingsWorkflow
                .ReadEntryOfferHeardIsoCodeAsync(cancellationToken)
                .ConfigureAwait(false);
            return !string.Equals(heardIn, selectedIsoCode, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not VoiceInputPausedException)
        {
            dependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                ex,
                "Could not read whether the entry offer was already heard in {IsoCode}; offering it in full.",
                selectedIsoCode);
            return true;
        }
    }

    /// <summary>
    /// Schreibt den Sprachmerker und protokolliert gewöhnliche Speicherfehler, damit sie den
    /// anschließenden Start nicht verhindern. Abbruch- und Pausensignale werden weitergegeben.
    /// </summary>
    public async ValueTask RememberOfferHeardAsync(string isoCode, CancellationToken cancellationToken)
    {
        try
        {
            await dependencies.UserLanguageSettingsWorkflow
                .SaveEntryOfferHeardAsync(isoCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not VoiceInputPausedException)
        {
            dependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                ex,
                "Could not record that the entry offer was heard in {IsoCode}; it will be offered in full again.",
                isoCode);
        }
    }
}
