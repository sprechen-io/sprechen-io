namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class InMemoryBrowserDebugConsole : IBrowserDebugConsole
{
    private readonly object _gate = new();
    private bool _enabled;
    private readonly List<string> _entries = [];

    public bool Enabled
    {
        get
        {
            lock (_gate)
            {
                return _enabled;
            }
        }
    }

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    public ValueTask<bool> GetEnabledAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(_enabled);
        }
    }

    /// <summary>
    /// Erlaubt Berichte unabhängig von der standardmäßig ausgeschalteten Debugausgabe.
    /// Tests können diese Freigabe getrennt setzen.
    /// </summary>
    public bool DiagnosticsAdmitted { get; set; } = true;

    public ValueTask<bool> GetDiagnosticsAdmittedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(DiagnosticsAdmitted);
    }

    public ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _enabled = enabled;
        }

        return ValueTask.CompletedTask;
    }

    public bool DiagnosticSession { get; private set; }

    public DiagnosticSessionState? LastDiagnosticSession { get; private set; }

    public ValueTask SetDiagnosticSessionAsync(DiagnosticSessionState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        DiagnosticSession = state.Enabled;
        LastDiagnosticSession = state;
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteAsync(string scope, string message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_enabled || DiagnosticSession)
            {
                _entries.Add($"{scope.Trim()}: {message.Trim()}");
            }
        }

        return ValueTask.CompletedTask;
    }

    public Task WaitForEntryAsync(string expectedEntry, TimeSpan timeout)
    {
        return WaitForEntryAsync(expectedEntry, timeout, CancellationToken.None);
    }

    public async Task WaitForEntryAsync(
        string expectedEntry,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        int firstEntryIndex = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedEntry);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive.");
        }

        var deadlineUtc = DateTimeOffset.UtcNow + timeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                if (_entries.Skip(firstEntryIndex).Contains(expectedEntry, StringComparer.Ordinal))
                {
                    return;
                }
            }

            if (DateTimeOffset.UtcNow >= deadlineUtc)
            {
                throw new TimeoutException($"Browser debug entry was not observed within {timeout}: {expectedEntry}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
        }
    }
}
