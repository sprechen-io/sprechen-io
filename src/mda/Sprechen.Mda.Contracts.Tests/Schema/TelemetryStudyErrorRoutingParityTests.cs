using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Mda.Contracts.Tests.Schema;

public sealed class TelemetryStudyErrorRoutingParityTests
{
    [Fact]
    public void NoRoutedCodeReachesAStudyLineAsItsOwnDiagnosis()
    {
        // Die Zuordnung bestimmt das Ereignis; dessen Diagnosecode steht fest.
        // Interne Fehlercodes und Diagnosecodes bleiben getrennt, damit sie nicht verwechselt werden.
        var overlap = TelemetryStudyErrorRouting.ByErrorCode.Keys
            .Where(code => TelemetryStudyDiagnosisCodes.All.Contains(code))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            overlap.Length == 0,
            "These internal codes share a name with a study diagnosis code:\n" + string.Join("\n", overlap));
    }
}
