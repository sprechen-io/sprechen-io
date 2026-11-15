namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Speichert die beobachtete Ausgangslatenz, damit die Aufnahme den hörbaren
/// Nachlauf der eigenen Sprachausgabe berücksichtigen kann.
/// </summary>
internal sealed class AssistantPlaybackOutputLatencyState : IAssistantPlaybackOutputLatencyState
{
    private readonly object _gate = new();
    private double _outputLatencyMs;

    public double OutputLatencyMs
    {
        get
        {
            lock (_gate)
            {
                return _outputLatencyMs;
            }
        }
    }

    public void SetOutputLatencyMs(double outputLatencyMs)
    {
        // Nicht-endliche oder negative Werte gelten als unbekannte Ausgangslatenz (0 Millisekunden).
        var sanitized = double.IsFinite(outputLatencyMs) && outputLatencyMs > 0
            ? outputLatencyMs
            : 0;

        lock (_gate)
        {
            _outputLatencyMs = sanitized;
        }
    }
}
