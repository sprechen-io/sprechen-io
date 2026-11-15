namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Zuletzt beobachtete Ausgangslatenz in Millisekunden. Sie beschreibt den hörbaren
/// Nachlauf durch die Ausgabepuffer. Die Aufnahme kann damit ihre Schutzfrist
/// nach der Wiedergabe verlängern. Der Standardwert 0 fügt keine zusätzliche Zeit hinzu.
/// </summary>
public interface IAssistantPlaybackOutputLatencyState
{
    double OutputLatencyMs { get; }

    void SetOutputLatencyMs(double outputLatencyMs);
}
