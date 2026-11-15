using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft die aufgeführten Enum-Werte gegen das modellierte Telemetrievokabular.
/// Damit richten sich die Erwartungen nach dem Modell und nicht nach Enum-Namen.
/// </summary>
public sealed class WakeWordTelemetryEventCodesTests
{
    [Theory]
    [InlineData(WakeWordMissPhase.MissTrace, TelemetryWakeWordMissPhases.MissTrace)]
    [InlineData(WakeWordMissPhase.MissDiagnostic, TelemetryWakeWordMissPhases.MissDiagnostic)]
    [InlineData(WakeWordMissPhase.EarconFailed, TelemetryWakeWordMissPhases.EarconFailed)]
    [InlineData(WakeWordMissPhase.RecordingCleanupFailed, TelemetryWakeWordMissPhases.RecordingCleanupFailed)]
    [InlineData(WakeWordMissPhase.GuidanceSpoken, TelemetryWakeWordMissPhases.GuidanceSpoken)]
    public void ToPhaseValue_MapsEveryMissPhaseToItsModeledValue(WakeWordMissPhase phase, string expected)
        => Assert.Equal(expected, WakeWordTelemetryEventCodes.ToPhaseValue(phase));

    [Theory]
    [InlineData(WakeWordActivationStatus.NoRecognizedSpeech, TelemetryWakeWordActivationStatuses.NoRecognizedSpeech)]
    [InlineData(WakeWordActivationStatus.MissingLeadingWakeWord, TelemetryWakeWordActivationStatuses.MissingLeadingWakeWord)]
    [InlineData(WakeWordActivationStatus.LeadingWakeWordDetected, TelemetryWakeWordActivationStatuses.LeadingWakeWordDetected)]
    public void ToStatusValue_MapsEveryActivationStatusToItsModeledValue(WakeWordActivationStatus status, string expected)
        => Assert.Equal(expected, WakeWordTelemetryEventCodes.ToStatusValue(status));

    [Fact]
    public void ToPhaseValue_RejectsAnUnknownPhase()
        => Assert.Throws<ArgumentOutOfRangeException>(() => WakeWordTelemetryEventCodes.ToPhaseValue((WakeWordMissPhase)999));

    [Fact]
    public void ToStatusValue_RejectsAnUnknownStatus()
        => Assert.Throws<ArgumentOutOfRangeException>(() => WakeWordTelemetryEventCodes.ToStatusValue((WakeWordActivationStatus)999));
}
