namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält fest, ob auf die Wiedergabe gewartet wurde, und bewahrt einen dabei
/// oder zuvor gesetzten Lesecursor, damit früh eingegangene Antworten erhalten bleiben.
/// </summary>
internal readonly record struct RecentPlaybackSettleOutcome(bool Waited, SampleCursor? SettleCursor);
