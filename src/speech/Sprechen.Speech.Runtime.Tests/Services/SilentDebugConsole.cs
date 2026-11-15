using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class SilentDebugConsole : IBrowserDebugConsole
{
    public ValueTask<bool> GetEnabledAsync(CancellationToken cancellationToken) => new(true);

    public ValueTask<bool> GetDiagnosticsAdmittedAsync(CancellationToken cancellationToken) => new(true);

    public ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask SetDiagnosticSessionAsync(DiagnosticSessionState state, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask WriteAsync(string scope, string message, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
