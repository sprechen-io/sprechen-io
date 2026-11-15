using Grpc.Core;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Sprechen.Web.HostSupport.Grpc.Proxy;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

/// <summary>
/// Prüft für jeden definierten Erkennungsabschluss mindestens LogLevel.Information.
/// Damit bleibt ein regulärer Abschluss auch bei deaktivierten Debug-Meldungen zum protokollierten Beginn sichtbar.
/// </summary>
public sealed class SpeechProxyStudyLevelReadabilityTests
{
    /// <summary>
    /// Leitet Testfälle aus allen Namen des internen Abschluss-Enums ab.
    /// Neue Enumwerte erscheinen automatisch als Testfälle und schlagen in WriteTerminal
    /// fehl, bis ihre Erzeugung ergänzt ist.
    /// </summary>
    public static TheoryData<string> EveryTerminalThereIs
        => new(Enum.GetNames<SpeechProxyRecognitionTerminal>());

    [Theory]
    [MemberData(nameof(EveryTerminalThereIs))]
    public void EveryTerminal_IsReadableWithoutDebug(string terminalName)
    {
        var terminal = Enum.Parse<SpeechProxyRecognitionTerminal>(terminalName);
        var logger = new RecordingLogger<SpeechProxyStudyLevelReadabilityTests>();

        WriteTerminal(new SpeechProxySpeechToTextCompletionLogger(logger), terminal);

        var entry = Assert.Single(logger.Entries);
        Assert.True(
            entry.Level >= LogLevel.Information,
            $"terminal {terminal} was written at {entry.Level}; at a study level its opening would be "
            + "on the record with nothing to pair against, which reads as a stream that never ended");
    }

    private static void WriteTerminal(
        SpeechProxySpeechToTextCompletionLogger completionLogger,
        SpeechProxyRecognitionTerminal terminal)
    {
        var forwardedAudio = new SpeechProxyForwardedAudio();
        var callContext = new SpeechProxyCallContext("corr-1", "trace-1", new Metadata(), "sess-1");

        switch (terminal)
        {
            case SpeechProxyRecognitionTerminal.Transcript:
                completionLogger.LogCompleted(
                    new Sprechen.Proto.SpeechTranscript { Text = "Max" }, forwardedAudio, callContext);
                break;
            case SpeechProxyRecognitionTerminal.Empty:
                completionLogger.LogCompleted(
                    new Sprechen.Proto.SpeechTranscript(), forwardedAudio, callContext);
                break;
            case SpeechProxyRecognitionTerminal.CallerGone:
                completionLogger.LogCallerGone(forwardedAudio, callContext);
                break;
            case SpeechProxyRecognitionTerminal.Failed:
                completionLogger.LogFailed(new InvalidOperationException("boom"), forwardedAudio, callContext);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(terminal), terminal, "a terminal with no way to produce it cannot be asserted on");
        }
    }
}
