namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet die Startentscheidung von <see cref="AssistantPlaybackCoordinator"/> mit dem
/// <see cref="SpeechCaptureStartContext"/> und einem optionalen Lesecursor nach der Wiedergabe.
/// </summary>
/// <remarks>
/// Im Leerlauf nach Wiedergabe kann der Cursor bereits vor Ablauf der Schutzfrist gesetzt sein.
/// Die folgende Aufnahme liest dann die seitdem eingetroffenen Audiodaten nach, damit eine früh
/// begonnene Antwort nicht verloren geht. In den übrigen Kontexten ist der Cursor <c>null</c>.
/// Es wird keine zusätzliche Position zum Nachlesen vorgegeben.
/// </remarks>
internal readonly record struct CaptureStartResolution(
    SpeechCaptureStartContext Context,
    SampleCursor? PlaybackSettleLookbackCursor);
