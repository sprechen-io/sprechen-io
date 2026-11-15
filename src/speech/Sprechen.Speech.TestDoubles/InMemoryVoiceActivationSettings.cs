namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Hält die im Test gesetzten Aktivierungsformen im Speicher.
/// Lesen und Schreiben kopieren die Arrays, damit Aufrufer den Vorrat nicht nachträglich ändern.
/// </summary>
public sealed class InMemoryVoiceActivationSettings : IVoiceActivationSettings
{
    private readonly object _gate = new();
    private string[] _wakeWordActivationForms = [];

    public ValueTask<string[]> GetWakeWordActivationFormsOverrideAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(_wakeWordActivationForms.ToArray());
        }
    }

    public ValueTask SetWakeWordActivationFormsOverrideAsync(string[] wakeWordActivationForms, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _wakeWordActivationForms = wakeWordActivationForms.ToArray();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ClearWakeWordActivationFormsOverrideAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _wakeWordActivationForms = [];
        }

        return ValueTask.CompletedTask;
    }
}
