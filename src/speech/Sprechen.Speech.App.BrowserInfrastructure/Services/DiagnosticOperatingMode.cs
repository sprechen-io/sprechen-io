using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Hält den vom Einstieg bestimmten Diagnosemodus samt Korrelationskennungen
/// für die Sitzung zusammen, damit Browser und Berichte dieselbe Zuordnung verwenden.
/// </summary>
internal sealed class DiagnosticOperatingMode : IDiagnosticOperatingMode
{
    private readonly ISpeechSessionIdentity _sessionIdentity;
    private readonly IBrowserDebugConsole _browserDebugConsole;
    private readonly string _clientCorrelationId = Guid.NewGuid().ToString("N");

    public DiagnosticOperatingMode(
        ISpeechSessionIdentity sessionIdentity,
        IBrowserDebugConsole browserDebugConsole)
    {
        _sessionIdentity = sessionIdentity ?? throw new ArgumentNullException(nameof(sessionIdentity));
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
    }

    public bool IsDiagnostic { get; private set; }

    public string SessionId => _sessionIdentity.SessionId;

    public string ClientCorrelationId => _clientCorrelationId;

    public string OperatingModeMarker =>
        IsDiagnostic ? DiagnosticOperatingModeNames.Diagnostic : DiagnosticOperatingModeNames.Standard;

    public void Activate(bool isDiagnostic)
    {
        IsDiagnostic = isDiagnostic;
        _ = _browserDebugConsole.SetDiagnosticSessionAsync(
            new DiagnosticSessionState(
                Enabled: IsDiagnostic,
                SessionId: SessionId,
                ClientCorrelationId: ClientCorrelationId,
                OperatingMode: OperatingModeMarker),
            CancellationToken.None);
    }
}
