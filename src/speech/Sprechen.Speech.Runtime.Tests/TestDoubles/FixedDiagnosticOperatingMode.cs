using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FixedDiagnosticOperatingMode : IDiagnosticOperatingMode
{
    public bool IsDiagnostic { get; set; }

    public string SessionId { get; set; } = "session-test";

    public string ClientCorrelationId { get; set; } = "cid-test";

    public string OperatingModeMarker =>
        IsDiagnostic ? DiagnosticOperatingModeNames.Diagnostic : DiagnosticOperatingModeNames.Standard;

    public void Activate(bool isDiagnostic)
    {
        IsDiagnostic = isDiagnostic;
    }
}
