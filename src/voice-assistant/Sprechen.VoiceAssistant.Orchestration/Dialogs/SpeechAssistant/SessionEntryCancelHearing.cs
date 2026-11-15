using Microsoft.Extensions.Logging;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft eine nicht angenommene Antwort auf eine fremdsprachige Einstiegsfrage auf Abbruch in der
/// Sitzungssprache. Ein bereits passendes Transkript entscheidet direkt. Andernfalls wird die
/// vorhandene Aufnahme einmal mit dem Erkennungsmodell der Sitzungssprache erneut erkannt.
/// Das benötigt einen weiteren Erkennungsauftrag, eröffnet aber kein neues Antwortfenster.
/// Technische Fehler werden protokolliert, soweit sie bis hierher weitergegeben werden.
/// Ein intern behandelter Erkennungsabbruch liefert dagegen nur keinen Treffer.
/// </summary>
internal sealed class SessionEntryCancelHearing
{
    private readonly SessionEntryQuestion _question;
    private readonly ILogger _logger;

    public SessionEntryCancelHearing(SessionEntryQuestion question, ILogger logger)
    {
        _question = question ?? throw new ArgumentNullException(nameof(question));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<string?> HearAsync(
        SessionEntryAnswer answer,
        string sessionIsoCode,
        IReadOnlyList<string> cancelUtterances,
        CancellationToken cancellationToken)
    {
        if (!answer.HasSpeech)
        {
            return null;
        }

        if (SessionEntryCancelReader.IsCancelled(answer.Transcript, cancelUtterances))
        {
            return "foreignRungFirstTranscript";
        }

        try
        {
            var secondTranscript = await _question
                .RecognizeAgainUnderSessionLanguageAsync(
                    answer.Utterance!,
                    sessionIsoCode,
                    cancelUtterances,
                    cancellationToken)
                .ConfigureAwait(false);

            return SessionEntryCancelReader.IsCancelled(secondTranscript, cancelUtterances)
                ? "foreignRungSecondHearing"
                : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            and not VoiceInputPausedException)
        {
            _logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                ex,
                "Session-entry cancel read under {IsoCode} could not run; offering the next language.",
                sessionIsoCode);
            return null;
        }
    }
}
