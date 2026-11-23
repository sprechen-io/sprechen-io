using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

internal sealed class PcmAdmissionObserver(Action onAccepted, CancellationToken cancellationToken)
{
    private Action? _onAccepted = onAccepted;

    [JSInvokable]
    public void OnAccepted()
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Exchange(ref _onAccepted, null)?.Invoke();
    }
}
