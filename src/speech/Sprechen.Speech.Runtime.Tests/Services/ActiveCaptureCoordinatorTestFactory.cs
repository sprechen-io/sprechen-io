using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Erzeugt den Aufnahmekoordinator mit echtem Segmentierer und lokalem Audiopuffer
/// für Tests des Aufnahmeablaufs.
/// </summary>
internal static class ActiveCaptureCoordinatorTestFactory
{
    internal static SpeechCaptureActiveCaptureCoordinator Create(
        IBrowserDebugConsole browserDebugConsole,
        ICaptureDiagnosticsReporter reporter,
        IMicGateState micGateState)
    {
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            }));

        return new SpeechCaptureActiveCaptureCoordinator(
            new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict).Object,
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            browserDebugConsole,
            new SpeechCaptureSegmenterFactory(optionsFactory),
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            micGateState,
            reporter);
    }
}
