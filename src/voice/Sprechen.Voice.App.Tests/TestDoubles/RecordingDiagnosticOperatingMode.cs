using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class RecordingDiagnosticOperatingMode : IDiagnosticOperatingMode
{
    public bool IsDiagnostic { get; private set; }

    public string SessionId { get; } = "session-test";

    public string ClientCorrelationId { get; } = "cid-test";

    public string OperatingModeMarker =>
        IsDiagnostic ? DiagnosticOperatingModeNames.Diagnostic : DiagnosticOperatingModeNames.Standard;

    public int ActivateCalls { get; private set; }

    public void Activate(bool isDiagnostic)
    {
        ActivateCalls++;
        IsDiagnostic = isDiagnostic;
    }
}
