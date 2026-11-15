namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Fortsetzungsart eines Feldzugs mit Transkript, Erkennungssicherheit
/// und optionalem Endergebnis für die Anwendung zusammen.
/// </summary>
public sealed record FormVoiceCommandFieldOrchestrationResult(
    FormVoiceCommandFieldHandlingResult? FinalResult,
    string? TranscriptForApply,
    AmbiguityTelemetryData? AmbiguityTelemetry,
    int? SttConfidencePercent)
{
    public bool HasFinalResult => FinalResult is not null;

    public string RequiredTranscriptForApply
        => TranscriptForApply ?? throw new InvalidOperationException("Transcript for apply is not available.");

    /// <summary>
    /// Kennzeichnet eine Fortsetzung ohne Endergebnis mit leerem Anwendungstext
    /// für den besonderen Anfrageweg der leeren Auswahl.
    /// </summary>
    public bool IsEmptySelectionContinuation
        => FinalResult is null && TranscriptForApply is { Length: 0 };

    /// <summary>
    /// Kennzeichnet einen bereits zusammengesetzten Wert für den Anwendungspfad
    /// ohne erneutes Lesen durch die Buchstaben- oder Zifferngrammatik.
    /// </summary>
    public bool IsComposedValueContinuation { get; init; }

    /// <summary>
    /// Übernimmt die vom Aufrufer angegebene ausdrückliche Annahme einer
    /// Mehrfachauswahl für die nachfolgende Bestätigung. Andere Erzeugungsmethoden
    /// lassen das Kennzeichen standardmäßig ungesetzt.
    /// </summary>
    public bool FinalizedByExplicitAccept { get; init; }

    public static FormVoiceCommandFieldOrchestrationResult Completed(FormVoiceCommandFieldHandlingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new FormVoiceCommandFieldOrchestrationResult(result, null, null, null);
    }

    public static FormVoiceCommandFieldOrchestrationResult ContinueWithTranscript(
        string transcriptForApply,
        AmbiguityTelemetryData? ambiguityTelemetry,
        int? sttConfidencePercent,
        bool finalizedByExplicitAccept = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcriptForApply);
        return new FormVoiceCommandFieldOrchestrationResult(
            FinalResult: null,
            TranscriptForApply: transcriptForApply,
            AmbiguityTelemetry: ambiguityTelemetry,
            SttConfidencePercent: sttConfidencePercent)
        {
            FinalizedByExplicitAccept = finalizedByExplicitAccept
        };
    }

    /// <summary>
    /// Erzeugt die Fortsetzung mit einem zusammengesetzten Wert. Dessen
    /// Anwendungspfad kann weiterhin normalisieren und validieren, ohne ihn
    /// erneut durch die Buchstaben- oder Zifferngrammatik zu lesen.
    /// </summary>
    public static FormVoiceCommandFieldOrchestrationResult ContinueWithComposedValue(
        string composedValue,
        int? sttConfidencePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(composedValue);
        return new FormVoiceCommandFieldOrchestrationResult(
            FinalResult: null,
            TranscriptForApply: composedValue,
            AmbiguityTelemetry: null,
            SttConfidencePercent: sttConfidencePercent)
        {
            IsComposedValueContinuation = true
        };
    }

    /// <summary>
    /// Erzeugt die ausdrücklich leere Fortsetzung für eine abgeschlossene
    /// Mehrfachauswahl. Der gewöhnliche Transkriptweg verlangt dagegen nichtleeren Text.
    /// </summary>
    public static FormVoiceCommandFieldOrchestrationResult ContinueWithEmptySelection(int? sttConfidencePercent)
        => new(
            FinalResult: null,
            TranscriptForApply: string.Empty,
            AmbiguityTelemetry: null,
            SttConfidencePercent: sttConfidencePercent);
}

