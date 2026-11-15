namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bindet einen fachlichen Parser an die gemeinsame Verarbeitung von
/// Bestätigungsantworten an. Zustandswechsel und Freigabe werden anschließend
/// anhand der Bestätigungsregeln entschieden.
/// </summary>
public sealed class SelectionDialogConfirmationHandler<TSelection>(
    Func<string, SelectionDialogCommonIntentParseResult> parseIntent)
    where TSelection : class
{
    private readonly Func<string, SelectionDialogCommonIntentParseResult> _parseIntent =
        parseIntent ?? throw new ArgumentNullException(nameof(parseIntent));

    /// <summary>
    /// Übersetzt ein nicht leeres erkanntes Transkript in eine gemeinsame Absicht samt optionalem
    /// Kandidatenziel.
    /// </summary>
    public Task<SelectionDialogCommonIntentParseResult> ParseAsync(
        string transcript,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _parseIntent(transcript)
            ?? throw new SelectionCandidateStateException("The confirmation parser returned no result."));
    }
}
