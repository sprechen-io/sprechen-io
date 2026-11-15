namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Werttext, erwarteten Eingabemodus und Herkunftsangaben für die
/// Übernahme eines Feldwerts. Eigene Erzeugungsmethoden kennzeichnen leere
/// Mehrfachauswahlen und bereits zusammengesetzte Werte.
/// </summary>
internal sealed record FormVoiceCommandFieldApplyRequest
{
    public FormVoiceCommandFieldApplyRequest(
        InputMode expectedInputMode,
        string transcriptForApply,
        AmbiguityTelemetryData? ambiguityTelemetry,
        int? sttConfidencePercent,
        bool finalizedByExplicitAccept = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcriptForApply);

        ExpectedInputMode = expectedInputMode;
        TranscriptForApply = transcriptForApply;
        AmbiguityTelemetry = ambiguityTelemetry;
        SttConfidencePercent = sttConfidencePercent;
        FinalizedByExplicitAccept = finalizedByExplicitAccept;
    }

    private FormVoiceCommandFieldApplyRequest(InputMode expectedInputMode, int? sttConfidencePercent)
    {
        ExpectedInputMode = expectedInputMode;
        TranscriptForApply = string.Empty;
        AmbiguityTelemetry = null;
        SttConfidencePercent = sttConfidencePercent;
    }

    /// <summary>
    /// Erzeugt die ausdrücklich gekennzeichnete Anwendung einer leeren
    /// Mehrfachauswahl. Andere Konstruktionswege verlangen weiterhin nichtleeren Text.
    /// </summary>
    public static FormVoiceCommandFieldApplyRequest ForEmptySetSelection(
        InputMode expectedInputMode,
        int? sttConfidencePercent)
        => new(expectedInputMode, sttConfidencePercent);

    /// <summary>
    /// Erzeugt eine Anforderung für einen fertig zusammengesetzten Wert. Der
    /// Anwendungspfad umgeht die Buchstaben- und Zifferngrammatik, kann den Wert
    /// aber weiterhin normalisieren und validieren.
    /// </summary>
    public static FormVoiceCommandFieldApplyRequest ForComposedValue(
        InputMode expectedInputMode,
        string composedValue,
        int? sttConfidencePercent)
        => new(expectedInputMode, composedValue, null, sttConfidencePercent)
        {
            IsComposedValue = true
        };

    /// <summary>
    /// Kennzeichnet einen fertig zusammengesetzten Wert für den dafür vorgesehenen
    /// Anwendungspfad ohne erneutes Lesen durch die Buchstaben- oder Zifferngrammatik.
    /// </summary>
    public bool IsComposedValue { get; init; }

    public InputMode ExpectedInputMode { get; }

    public string TranscriptForApply { get; }

    public AmbiguityTelemetryData? AmbiguityTelemetry { get; }

    public int? SttConfidencePercent { get; }

    /// <summary>
    /// Kennzeichnet die vom Aufrufer angegebene ausdrückliche Annahme innerhalb
    /// der Mehrfachauswahl für die anschließende Anwendungsentscheidung.
    /// </summary>
    public bool FinalizedByExplicitAccept { get; }
}
