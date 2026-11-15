
namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemoryVoiceActivationSettings : IVoiceActivationSettings
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

