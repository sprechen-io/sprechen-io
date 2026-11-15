using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Führt durch Änderungswunsch, Diktat und Bestätigung der Aktivierungsformen,
/// damit neue Formen erst nach ausdrücklicher Bestätigung gespeichert werden.
/// </summary>
internal sealed class WakeWordSetupWorkflow(
    IWakeWordSetupScenarioFactory scenarioFactory,
    IWakeWordSetupSpeechInteractor speechInteractor,
    IVoiceActivationSettings voiceActivationSettings,
    ILogger<WakeWordSetupWorkflow> logger) : IWakeWordSpeechSetupRunner
{
    private const int MaxYesNoAttempts = 3;
    private const int MaxWakeWordAttempts = 3;

    private readonly IWakeWordSetupScenarioFactory _scenarioFactory =
        scenarioFactory ?? throw new ArgumentNullException(nameof(scenarioFactory));

    private readonly IVoiceActivationSettings _voiceActivationSettings =
        voiceActivationSettings ?? throw new ArgumentNullException(nameof(voiceActivationSettings));

    private readonly IWakeWordSetupSpeechInteractor _speechInteractor =
        speechInteractor ?? throw new ArgumentNullException(nameof(speechInteractor));

    private readonly ILogger<WakeWordSetupWorkflow> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task EnsureWakeWordsConfiguredAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var scenario = await _scenarioFactory.CreateAsync(isoCode, cancellationToken).ConfigureAwait(false);
        if (scenario is null)
        {
            var normalizedIsoCode = IsoCode.TryNormalizeIso6391(isoCode) ?? string.Empty;
            _logger.LogDebug("No wake words available for language '{IsoCode}'.", normalizedIsoCode);
            return;
        }

        var wantsChange = await _speechInteractor
            .PromptYesNoAsync(scenario.ChangePrompt, scenario.IsoCode, MaxYesNoAttempts, cancellationToken)
            .ConfigureAwait(false);
        if (wantsChange is null)
        {
            // `null` bedeutet, dass keine gültige Ja/Nein-Antwort vorliegt, nicht Verneinung.
            // Die bisherigen Formen bleiben ohne weitere Ansage erhalten, weil die vorhandene
            // Nichterkennungsansage zum Diktatschritt gehört und dessen Scheitern behaupten würde.
            _logger.LogWarning(
                "Wake-word setup could not obtain a yes/no answer after {Attempts} attempts; keeping the current activation forms without telling the listener.",
                MaxYesNoAttempts);
            return;
        }

        if (wantsChange is false)
        {
            return;
        }

        for (var attempt = 0; attempt < MaxWakeWordAttempts; attempt++)
        {
            var captureResult = await _speechInteractor
                .PromptSpeechToTextAsync(scenario.AskForNewWakeWordsPrompt, scenario.IsoCode, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
            {
                _logger.LogWarning(
                    "Wake-word setup speech capture failed (code={Code}, recoverable={Recoverable}).",
                    captureResult.FailureCode,
                    captureResult.Recoverable);

                // Nach einem Aufnahmefehler wird erneut nach neuen Formen gefragt. Das fehlende
                // Transkript darf nicht als leeres Diktatergebnis gelten: Die Nichterkennungsansage
                // würde ein Missverstehen behaupten, obwohl die Aufnahme oder Erkennung gescheitert ist.
                continue;
            }

            var wakeWordActivationForms = new WakeWordActivationFormCodec().Parse(captureResult.Transcript);
            if (wakeWordActivationForms.Length == 0)
            {
                await _speechInteractor.SpeakAsync(scenario.NotRecognizedPrompt, scenario.IsoCode, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            var confirmed = await _speechInteractor
                .PromptYesNoAsync(
                    scenario.CreateConfirmationPrompt(wakeWordActivationForms),
                    scenario.IsoCode,
                    MaxYesNoAttempts,
                    cancellationToken)
                .ConfigureAwait(false);
            if (confirmed is not true)
            {
                continue;
            }

            await _voiceActivationSettings.SetWakeWordActivationFormsOverrideAsync(wakeWordActivationForms, cancellationToken).ConfigureAwait(false);
            await _speechInteractor.SpeakAsync(scenario.CreateSavedPrompt(wakeWordActivationForms), scenario.IsoCode, cancellationToken)
                .ConfigureAwait(false);
            return;
        }
    }
}
