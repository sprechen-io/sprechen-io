using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Einstellungen und Ausführungsdienste eines Auswahllaufs zusammen, damit die Phasen denselben
/// Anfangsbestand und Dialogkontext verwenden.
/// </summary>
public sealed class SelectionDialogExecutionSettings<TSelection>
    where TSelection : class
{
    private static readonly SpeechAssistantTraceContext DefaultTrace =
        new(null, null, SpeechAssistantTraceScope.SelectionDialogRuntime);

    public SelectionDialogExecutionSettings(
        SelectionDialogSpeechTurnRunner speech,
        string isoCode,
        SelectionCandidateSnapshot<TSelection> initialCandidates,
        SelectionDialogInteractionSettings interactionSettings,
        int maxRetries,
        int noInputTimeoutMs,
        SpeechTranscript? initialListeningTranscript = null,
        SpeechAssistantTraceContext? trace = null,
        ISelectionTurnKeyterms? keyterms = null)
    {
        Speech = speech ?? throw new ArgumentNullException(nameof(speech));
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(initialCandidates);
        ArgumentNullException.ThrowIfNull(interactionSettings);

        IsoCode = isoCode;
        InitialCandidates = initialCandidates;
        InteractionSettings = interactionSettings;
        MaxRetries = maxRetries;
        NoInputTimeoutMs = noInputTimeoutMs;
        InitialListeningSpeechTranscript = initialListeningTranscript is null
            ? null
            : initialListeningTranscript with
            {
                Text = string.IsNullOrWhiteSpace(initialListeningTranscript.Text)
                    ? throw new ArgumentException("Initial listening transcript text is required.", nameof(initialListeningTranscript))
                    : initialListeningTranscript.Text.Trim()
            };
        Trace = trace ?? DefaultTrace;
        Keyterms = keyterms;
    }

    public SelectionDialogSpeechTurnRunner Speech { get; }

    public string IsoCode { get; }

    public SelectionCandidateSnapshot<TSelection> InitialCandidates { get; }

    public SelectionDialogInteractionSettings InteractionSettings { get; }

    public int MaxRetries { get; }

    public int NoInputTimeoutMs { get; }

    /// <summary>
    /// Stellt den an den Rändern bereinigten Text des optionalen Anfangstranskripts bereit.
    /// </summary>
    public string? InitialListeningTranscript => InitialListeningSpeechTranscript?.Text;

    /// <summary>
    /// Stellt das optionale Anfangstranskript samt Vertrauenswert des Erkenners bereit.
    /// </summary>
    public SpeechTranscript? InitialListeningSpeechTranscript { get; }

    public SpeechAssistantTraceContext Trace { get; }

    /// <summary>
    /// Stellt die Quelle phasen- und stufenbezogener Erkennungshinweise bereit. Ohne Quelle werden
    /// hier keine zusätzlichen Hinweise angefordert. Mögliche Vorgaben des Erkennungsdienstes bleiben
    /// davon unabhängig.
    /// </summary>
    public ISelectionTurnKeyterms? Keyterms { get; }

    public ISpeechAssistantTrace? SpeechAssistantTrace => Trace.Trace;

    public string? CorrelationId => Trace.CorrelationId;

    public SpeechAssistantTraceScope TraceScope => Trace.Scope;
}
