using System.Reflection;
using System.Text.Json;

namespace Sprechen.Speech.Runtime.Tests.Schema;

public sealed class WakeWordTelemetrySchemaAlignmentTests
{

    [Theory]
    [MemberData(nameof(UndefinedPhaseProjections))]
    public void WakeWordTelemetryEventCodeProjection_FailsFastForUndefinedPhase(Action invoke)
    {
        Assert.Throws<ArgumentOutOfRangeException>(invoke);
    }

    public static IEnumerable<object[]> UndefinedPhaseProjections =>
    [
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordListenerSetupPhase)999))],
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordAcousticDetectionPhase)999))],
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordCaptureLoopPhase)999))],
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordTranscriptionRejectionPhase)999))],
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordStitchPhase)999))],
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordMissPhase)999))],
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordActivationPhase)999))],
        [new Action(() => WakeWordTelemetryEventCodes.ToEventCode((WakeWordFallbackPhase)999))],
    ];
}
