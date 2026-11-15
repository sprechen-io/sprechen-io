using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Kapselt die optionale Browserdiagnose und ihre Freigabe,
/// damit ausgefallene JavaScript-Diagnosefunktionen den Sprachdialog nicht unterbrechen.
/// </summary>
internal sealed class BrowserDebugConsole(IJSRuntime jsRuntime) : IBrowserDebugConsole
{
    private const string GetEnabledIdentifier = "browserDebugConsole.getEnabled";
    private const string GetDiagnosticsAdmittedIdentifier = "browserDebugConsole.getDiagnosticsAdmitted";
    private const string SetEnabledIdentifier = "browserDebugConsole.setEnabled";
    private const string SetDiagnosticSessionIdentifier = "browserDebugConsole.setDiagnosticSession";
    private const string WriteIdentifier = "browserDebugConsole.write";

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private bool _diagnosticSession;

    public ValueTask<bool> GetEnabledAsync(CancellationToken cancellationToken)
        => TryInvokeAsync(GetEnabledIdentifier, fallbackValue: false, cancellationToken);

    // Bei fehlender Browserantwort bleibt die Diagnose zugelassen,
    // damit eine ausgefallene Abfrage die Fehlerursache nicht verdeckt.
    public ValueTask<bool> GetDiagnosticsAdmittedAsync(CancellationToken cancellationToken)
        => TryInvokeAsync(GetDiagnosticsAdmittedIdentifier, fallbackValue: true, cancellationToken);

    public ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
        => TryInvokeVoidAsync(SetEnabledIdentifier, cancellationToken, enabled);

    public ValueTask SetDiagnosticSessionAsync(DiagnosticSessionState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        _diagnosticSession = state.Enabled;
        return TryInvokeVoidAsync(SetDiagnosticSessionIdentifier, cancellationToken, state);
    }

    public async ValueTask WriteAsync(string scope, string message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!_diagnosticSession && !await GetEnabledAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await TryInvokeVoidAsync(
                WriteIdentifier,
                cancellationToken,
                scope.Trim(),
                message.Trim())
            .ConfigureAwait(false);
    }

    private async ValueTask<T> TryInvokeAsync<T>(
        string identifier,
        T fallbackValue,
        CancellationToken cancellationToken,
        params object?[] args)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<T>(identifier, cancellationToken, args).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            return fallbackValue;
        }
    }

    private async ValueTask TryInvokeVoidAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            // Die optionale Browserdiagnose darf den Anwendungsablauf nicht unterbrechen.
        }
    }
}
