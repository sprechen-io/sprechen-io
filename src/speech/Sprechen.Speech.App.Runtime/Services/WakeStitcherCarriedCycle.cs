namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Bewahrt Text, Aufnahmezeit und Ringposition eines Abschnitts, damit die nächste Aufnahme eine unterbrochene Aktivierungsform ergänzen kann.
/// </summary>
internal sealed record WakeStitcherCarriedCycle(string TranscriptText, DateTimeOffset CapturedAt, SampleCursor Cursor);
