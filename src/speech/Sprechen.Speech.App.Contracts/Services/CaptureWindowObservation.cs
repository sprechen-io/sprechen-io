namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Beobachtungen eines Erfassungsfensters unabhängig davon, ob ein Sprachbeginn erkannt wurde.</summary>
/// <param name="ObservedFrames">Zahl der Frames, die den Segmentierer erreichten.
/// Ein positiver Wert belegt Audioeingang, aber nicht zwingend Sprache. 0 bedeutet keine beobachteten Frames.</param>
/// <param name="StartCursorHandedOff">Ob ein Cursor für den Rückblick vor Beginn der Live-Erfassung übergeben wurde.</param>
/// <param name="LookbackBytes">Zahl der vor der Live-Erfassung aus dem Ring gelesenen Bytes.
/// Der Wert ist 0, wenn kein Cursor übergeben wurde oder keine Daten daraus gelesen wurden.</param>
public sealed record CaptureWindowObservation(
    int ObservedFrames,
    bool StartCursorHandedOff,
    int LookbackBytes);
