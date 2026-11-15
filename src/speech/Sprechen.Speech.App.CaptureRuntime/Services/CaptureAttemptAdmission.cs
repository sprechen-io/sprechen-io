namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bewahrt den ersten Ungültigkeitsgrund einer Aufnahmezulassung samt Abbruchsignal,
/// damit ein vorbereiteter Versuch nach seiner Beendigung nicht erneut starten kann.
/// </summary>
internal sealed class CaptureAttemptAdmission : IDisposable
{
    private readonly CancellationTokenSource _terminalCancellation = new();
    private PendingCaptureAttemptTerminal? _terminal;

    internal CancellationToken TerminalCancellation => _terminalCancellation.Token;

    internal void Invalidate(PendingCaptureAttemptTerminal terminal)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        Interlocked.CompareExchange(ref _terminal, terminal, comparand: null);
    }

    internal void Cancel() => _terminalCancellation.Cancel();

    internal void ThrowIfInvalid() => Volatile.Read(ref _terminal)?.Throw();

    public void Dispose() => _terminalCancellation.Dispose();
}
