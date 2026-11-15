using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Serialisiert die Einführungsansagen zum Aktivierungswort und merkt ihren erfolgreichen Ablauf,
/// damit gewöhnliche Folgeaufrufe die Einführung nicht erneut anfordern.
/// </summary>
internal sealed class SpeechAssistantSessionOnboardingAnnouncer
{
    private readonly SpeechAssistantSetupDependencies _setupDependencies;
    private readonly SpeechAssistantInteractionDependencies _interactionDependencies;
    private readonly SemaphoreSlim _announcementGate = new(1, 1);
    private bool _announced;

    public SpeechAssistantSessionOnboardingAnnouncer(
        SpeechAssistantSetupDependencies setupDependencies,
        SpeechAssistantInteractionDependencies interactionDependencies)
    {
        _setupDependencies = setupDependencies ?? throw new ArgumentNullException(nameof(setupDependencies));
        _interactionDependencies = interactionDependencies ?? throw new ArgumentNullException(nameof(interactionDependencies));
    }

    public ValueTask AnnounceOnceAsync(string selectedIsoCode, CancellationToken cancellationToken)
        => AnnounceAsync(selectedIsoCode, repeatAfterLanguageChange: false, cancellationToken);

    /// <summary>
    /// Fordert die Einführung erneut in der übergebenen Sprache an, damit nach einem Sprachwechsel auch
    /// die Aktivierungsanweisung in dieser Sprache verfügbar wird.
    /// </summary>
    public ValueTask AnnounceAgainForChangedLanguageAsync(string selectedIsoCode, CancellationToken cancellationToken)
        => AnnounceAsync(selectedIsoCode, repeatAfterLanguageChange: true, cancellationToken);

    private async ValueTask AnnounceAsync(
        string selectedIsoCode,
        bool repeatAfterLanguageChange,
        CancellationToken cancellationToken)
    {
        // Die beiden Einführungsansagen serialisieren, damit konkurrierende Aufrufer sie nicht
        // überlagern. Der erste erfolgreiche Durchlauf setzt den Merker erst nach beiden
        // Wiedergabeaufrufen.
        await _announcementGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_announced && !repeatAfterLanguageChange)
            {
                return;
            }

            var politenessLevel = _setupDependencies.UserLanguageState.SelectedPolitenessLevel
                ?? PolitenessLevel.Formal;
            var intro = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(
                selectedIsoCode,
                politenessLevel);
            await _interactionDependencies.TextToSpeechPlayer
                .SpeakAsync(intro, selectedIsoCode, cancellationToken)
                .ConfigureAwait(false);

            var repeatHint = FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(
                selectedIsoCode,
                politenessLevel);
            await _interactionDependencies.TextToSpeechPlayer
                .SpeakAsync(repeatHint, selectedIsoCode, cancellationToken)
                .ConfigureAwait(false);

            _announced = true;
            _interactionDependencies.Logger.LogInformation(
                "Wake-word onboarding intro announced at session start for isoCode {IsoCode}.",
                selectedIsoCode);
        }
        catch (Exception ex)
        {
            // Den Wiedergabefehler weitergeben, damit der Aufrufer den fehlgeschlagenen
            // Einführungsversuch nicht als erfolgreichen Start behandelt.
            if (ex is not OperationCanceledException)
            {
                _interactionDependencies.Logger.LogDebug(
                    ex,
                    "Wake-word onboarding intro playback failed and remains pending for retry.");
            }

            throw;
        }
        finally
        {
            _announcementGate.Release();
        }
    }
}
