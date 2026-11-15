namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Übergibt nach dem Setzen eines Feldwerts den Ersatztext und die Kennzeichen
/// für die anschließende Bestätigung. Sie steuern, ob eine zusätzliche
/// Frage nötig ist und welche Korrekturmöglichkeiten angeboten werden.
/// </summary>
internal sealed record FormVoiceCommandConfirmationRequest
{
    public FormVoiceCommandConfirmationRequest(
        string transcriptForFallback,
        bool finalizedByExplicitAccept = false,
        bool fromSpellingOrDigits = false)
    {
        // Ein leerer Ersatztext ist bei einer ausdrücklich leeren optionalen
        // Mehrfachauswahl zulässig. Die Bestätigung kann den gespeicherten Wert
        // verwenden. Nur null wird hier abgewiesen.
        ArgumentNullException.ThrowIfNull(transcriptForFallback);

        TranscriptForFallback = transcriptForFallback;
        FinalizedByExplicitAccept = finalizedByExplicitAccept;
        FromSpellingOrDigits = fromSpellingOrDigits;
    }

    public string TranscriptForFallback { get; }

    /// <summary>
    /// Kennzeichnet eine bereits ausdrücklich übernommene Mehrfachauswahl.
    /// Die nachfolgende Verarbeitung kann dadurch eine zusätzliche allgemeine
    /// Bestätigungsfrage auslassen.
    /// </summary>
    public bool FinalizedByExplicitAccept { get; }

    /// <summary>
    /// Kennzeichnet einen Wert aus Buchstabier- oder Zifferneingabe.
    /// Bei seiner Bestätigung wird Buchstabieren nicht erneut angeboten.
    /// </summary>
    public bool FromSpellingOrDigits { get; }
}
