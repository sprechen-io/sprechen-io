namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Diagnosedaten eines Erfassungsversuchs bis zu seinem Abschluss.</summary>
/// <param name="CaptureSeq">Laufende Versuchsnummer innerhalb der Seitensitzung. Auch der kontinuierliche
/// Segmentstrom verbraucht Nummern ohne Einzelbericht. Lücken bedeuten daher nicht zwingend verlorene Berichte.</param>
/// <param name="Outcome">Ausgang des Versuchs: Sprache, Leerlauf, Abbruch oder Fehler.</param>
/// <param name="ArmedToOutcomeMs">Zeit vom Aufnahmebeginn bis zum Ausgang in Millisekunden.
/// Der Wert 0 kennzeichnet einen Versuch, dessen Erfassungsfenster nicht geöffnet wurde.</param>
/// <param name="CaptureMode">Erfassungsmodus des Versuchs, etwa Listening, ShortConfirmation oder PromptInterruption.</param>
/// <param name="StartContext">Wiedergabekontext beim Beginn, etwa kürzlich beendete eigene Wiedergabe.</param>
/// <param name="CapturedChunks">Bei einem Sprachergebnis die Zahl der Ergebnis-Chunks, sonst die Zahl beobachteter
/// Frames wie in <see cref="Window"/>. Die Anzahl ist keine Audiodauer. Ohne geöffnetes Erfassungsfenster
/// kann sie noch vom vorherigen Versuch stammen.</param>
/// <param name="PeakAmplitude">Höchste mittlere Absolutamplitude eines beobachteten Frames.
/// Der Wert 0 allein unterscheidet fehlende Frames nicht von einem sehr niedrigen Pegel.
/// Dazu <see cref="CaptureWindowObservation.ObservedFrames"/> und <see cref="ArmedToOutcomeMs"/> beachten.
/// Ohne geöffnetes Fenster kann der Wert noch vom vorherigen Versuch stammen.</param>
/// <param name="FaultKind">Typname des Fehlers bei einem fehlgeschlagenen Versuch, sonst null.
/// Er erlaubt die Unterscheidung von Fehlerarten ohne Übertragung der Ausnahmemeldung.</param>
/// <param name="Window">Beobachtete Frames, Übergabe eines Rückblick-Cursors und aus dem Ring gelesene Bytes.
/// Die Framezahl beginnt mit dem Erfassungsfenster, Cursorübergabe und Bytezahl schon davor.
/// Ein dazwischen beendeter Versuch kann deshalb neue Cursorwerte neben einer alten Framezahl melden.</param>
/// <param name="EstimatedFloor">Geschätzter Hintergrundpegel in denselben Einheiten wie <see cref="PeakAmplitude"/>.
/// Der Wert 0 gilt, solange kein adaptiver Pegel bestimmt wurde. Ohne neues Erfassungsfenster
/// kann noch der Wert des vorherigen Versuchs enthalten sein.</param>
/// <param name="AppliedOnsetThreshold">Verwendete Schwelle zur Erkennung eines Sprachbeginns.
/// Zusammen mit <see cref="PeakAmplitude"/> hilft sie, ein Leerlaufergebnis einzuordnen.
/// Der Wert 0 gilt, solange keine Schwelle bestimmt wurde. Ohne neues Erfassungsfenster
/// kann noch die Schwelle des vorherigen Versuchs enthalten sein.</param>
/// <param name="TurnId">Beim Beginn der Erfassung erzeugte Kennung zur Zuordnung von Erfassungsbericht,
/// Erkennungsanfrage und dienstseitigem Erkennungsdatensatz. Bei einem vorher gescheiterten Versuch null.</param>
public sealed record CaptureDiagnosticsReport(
    int CaptureSeq,
    string Outcome,
    double ArmedToOutcomeMs,
    string CaptureMode,
    string StartContext,
    int CapturedChunks,
    int PeakAmplitude,
    string? FaultKind = null,
    CaptureWindowObservation? Window = null,
    int EstimatedFloor = 0,
    int AppliedOnsetThreshold = 0,
    string? TurnId = null);
