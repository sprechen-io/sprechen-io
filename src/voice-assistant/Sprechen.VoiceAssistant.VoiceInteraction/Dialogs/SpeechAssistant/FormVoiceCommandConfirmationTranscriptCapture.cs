namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Nimmt die Antwort auf eine Feldbestätigung mit dem dafür vorgesehenen
/// Aufnahmemodus und Vokabular auf. Fehlende Sprache und vorübergehende
/// Aufnahmefehler liefern kein Transkript zurück.
/// </summary>
internal sealed class FormVoiceCommandConfirmationTranscriptCapture
{
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly ConfirmationKeytermVocabulary _keyterms = new();

    public FormVoiceCommandConfirmationTranscriptCapture(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task<string?> CaptureAsync(
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string prompt,
        CancellationToken cancellationToken,
        Action? onStartListening = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentNullException.ThrowIfNull(prompt);

        try
        {
            // Den Aufnahmemodus für kurze Bestätigungsantworten verwenden. Er behandelt
            // den Schutz vor gerade ausgegebener Sprache anders als die normale
            // Feldaufnahme. Die spätere Befehlserkennung bleibt davon unabhängig.
            var captureResult = await _speechService.CaptureConfirmationTranscriptAsync(
                    prompt,
                    context.IsoCode,
                    context.CaptureSettings,
                    onStartListening ?? (() => { }),
                    new SpeechCaptureTelemetryContext(session),
                    // Nur das für Bestätigungen vorgesehene Vokabular ohne allgemeine
                    // Serverbegriffe als Erkennungshinweise übergeben.
                    new SpeechKeyterms(
                        _keyterms.GetKeyterms(
                            context.Contract,
                            context.IsoCode,
                            context.VoiceSettings.GetPolitenessLevel()),
                        KeytermComposition.WithoutServerBase),
                    cancellationToken)
                .ConfigureAwait(false);

            return captureResult.Transcript;
        }
        catch (NoSpeechDetectedException)
        {
            return null;
        }
        catch (Exception ex) when (RecoverableCaptureFault.IsTransientCaptureFault(ex))
        {
            // Vorübergehende Aufnahmefehler ohne Transkript zurückgeben, damit der
            // aufrufende Bestätigungsablauf erneut nachfragen kann. Der Fehler wird
            // hier nicht als Ja oder Nein gedeutet.
            return null;
        }
    }
}
