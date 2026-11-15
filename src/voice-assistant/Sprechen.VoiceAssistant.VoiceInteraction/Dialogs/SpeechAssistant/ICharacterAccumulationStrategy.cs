namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die modusspezifischen Schritte der Zeichensammlung: Eingabe verarbeiten,
/// Befehle anwenden, Inhalte vorlesen und weitere Erfassung anfordern. Die Strategie
/// verwaltet ihren Puffer. Die wiederholte Ablaufsteuerung liegt beim gemeinsamen Treiber.
/// </summary>
internal interface ICharacterAccumulationStrategy
{
    /// <summary>
    /// Gibt an, ob der Puffer aktuell Inhalt enthält.
    /// </summary>
    bool HasContent { get; }

    /// <summary>
    /// Liefert die zusammengefasste Erkennungssicherheit der aktuell beitragenden
    /// Pufferinhalte oder null ohne gemeldete Sicherheit. Entfernte Beiträge zählen nicht weiter mit.
    /// </summary>
    int? AggregatedConfidencePercent { get; }

    /// <summary>
    /// Liefert den zusammengesetzten Wert für die anschließende Feldübernahme.
    /// </summary>
    string GetFinalizedValue();

    /// <summary>
    /// Verarbeitet einen Eingabeschritt und liefert Abschluss oder Fortsetzung an den
    /// Treiber zurück. Die nächste Erfassung gehört zu dessen Ablaufsteuerung.
    /// </summary>
    Task<CharacterAccumulationTurnOutcome> ProcessTurnAsync(
        CharacterAccumulationContext context,
        string transcript,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fordert eine Erfassung an und ordnet deren Ergebnis ein. Nichteingabe und
    /// Nichtverfügbarkeit des Erkenners müssen getrennt bleiben, weil der Treiber sie
    /// unterschiedlich behandelt. Eine technische Störung darf nicht als Abschlusspause gelten.
    /// </summary>
    Task<CharacterAccumulationTurnCapture> CaptureNextTurnAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fordert den modusspezifischen Fortsetzungshinweis an, bevor der Treiber
    /// nach einer möglichen Abschlusspause eine weitere Erfassung beginnt.
    /// </summary>
    Task AnnounceContinueOrDoneAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fordert den modusspezifischen Hinweis auf eine nicht verfügbare Erkennung an.
    /// Der Treiber bestimmt den Zeitpunkt. Die Strategie stellt die Rückmeldung bereit.
    /// </summary>
    Task AnnounceRecognizerUnavailableAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken);
}
