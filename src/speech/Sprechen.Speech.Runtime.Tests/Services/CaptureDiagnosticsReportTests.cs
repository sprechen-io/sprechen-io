using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.App.CaptureRuntime.Services;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class CaptureDiagnosticsReportTests
{
    [Fact]
    public async Task NoSpeech_IsReported_BecauseNothingElseCanSeeIt()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordStartAsync(
            silenceDebounceMs: 500,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening));
        await trace.RecordArmedAsync();
        await trace.RecordNoSpeechAsync();

        // Auch ein Aufnahmeversuch ohne anschließende Erkennungsanfrage braucht einen eigenen Bericht.
        var report = Assert.Single(reporter.Reports);
        Assert.Equal("no_speech", report.Outcome);
        Assert.Equal("Listening", report.CaptureMode);
        Assert.Equal(0, report.CapturedChunks);
        Assert.Equal(0, report.PeakAmplitude);
        Assert.Equal(0, report.Window!.ObservedFrames);
    }

    [Fact]
    public async Task PeakAmplitude_SeparatesASilentRoomFromADeafMicrophone()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        trace.ObservePeakAmplitude(180);
        trace.ObserveArrivedFrames(12);
        await trace.RecordNoSpeechAsync();

        // Ankommende Frames mit einem Pegel über null müssen von digitaler Stille unterscheidbar bleiben.
        var report = Assert.Single(reporter.Reports);
        Assert.Equal("no_speech", report.Outcome);
        Assert.Equal(180, report.PeakAmplitude);
        Assert.Equal(12, report.CapturedChunks);
        Assert.Equal(12, report.Window!.ObservedFrames);
    }

    [Fact]
    public async Task OnsetDecision_RidesOnTheReportNextToThePeak()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        trace.ObservePeakAmplitude(288);
        trace.ObserveArrivedFrames(183);
        trace.ObserveOnsetDecision(40, 350);
        await trace.RecordNoSpeechAsync();

        var report = Assert.Single(reporter.Reports);
        Assert.Equal(288, report.PeakAmplitude);
        Assert.Equal(40, report.EstimatedFloor);
        Assert.Equal(350, report.AppliedOnsetThreshold);
    }

    [Fact]
    public async Task DigitallySilentFrames_AreNotReportedAsNoSpeech()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        trace.ObserveArrivedFrames(40);
        await trace.RecordNoSpeechAsync();

        var report = Assert.Single(reporter.Reports);
        Assert.Equal("digital_silence", report.Outcome);
        Assert.Equal(0, report.PeakAmplitude);
        Assert.Equal(40, report.CapturedChunks);
        Assert.Equal(40, report.Window!.ObservedFrames);
    }

    [Fact]
    public async Task CursorHandoffAndLookback_RideOnTheReport()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordCursorHandoffAsync(new SampleCursor(24000));
        trace.ObserveLookbackBytes(4800);
        await trace.RecordArmedAsync();
        await trace.RecordNoSpeechAsync();

        var window = Assert.Single(reporter.Reports).Window!;
        Assert.True(window.StartCursorHandedOff);
        Assert.Equal(4800, window.LookbackBytes);
    }

    [Fact]
    public async Task Cancellation_AndNoSpeech_AreDistinguishable()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordStartAsync(
            silenceDebounceMs: 500,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ShortConfirmation));
        await trace.RecordArmedAsync();
        await trace.RecordCancellationAsync();

        // Ein Abbruch muss im Bericht von einem Versuch ohne erkannte Sprache unterscheidbar bleiben.
        var report = Assert.Single(reporter.Reports);
        Assert.Equal("cancelled", report.Outcome);
        Assert.Equal("ShortConfirmation", report.CaptureMode);
    }

    [Fact]
    public async Task ReporterFailure_DoesNotBreakTheCapture()
    {
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, new ThrowingReporter());

        await trace.RecordArmedAsync();

        // Optionale Diagnosefehler dürfen den Aufnahmeabschluss nicht scheitern lassen.
        await trace.RecordNoSpeechAsync();
    }

    [Fact]
    public async Task ReporterThatNeverCompletes_DoesNotOwnTheCaptureTerminal()
    {
        var reporter = new NeverCompletingCaptureDiagnosticsReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        var terminal = trace.RecordNoSpeechAsync().AsTask();

        await reporter.ReportStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await terminal.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Failure_CarriesItsCause_BecauseTheServerSeesNothingElse()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordFailureAsync(new InvalidOperationException("capture-fault"));

        // Der Ausnahmetyp macht die Fehlerart sichtbar, ohne die Ausnahmenachricht zu übertragen.
        var report = Assert.Single(reporter.Reports);
        Assert.Equal("failed", report.Outcome);
        Assert.Equal(nameof(InvalidOperationException), report.FaultKind);
    }

    [Fact]
    public async Task Failure_CarriesObservedFrameCount_NotALiteralZero()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        trace.ObserveArrivedFrames(17);
        await trace.RecordFailureAsync(new InvalidOperationException("capture-fault"));

        var report = Assert.Single(reporter.Reports);
        Assert.Equal("failed", report.Outcome);
        Assert.Equal(17, report.CapturedChunks);
        Assert.Equal(17, report.Window!.ObservedFrames);
        Assert.Equal(nameof(InvalidOperationException), report.FaultKind);
    }

    [Fact]
    public async Task Failure_BeforeArming_ReportsZeroDuration_AndSaysSoThroughTheFaultKind()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        // Kein RecordArmedAsync: der Fehler trifft vor dem Scharfschalten, wie beim Mic-Gate.
        await trace.RecordFailureAsync(new InvalidOperationException("boom"));

        var report = Assert.Single(reporter.Reports);
        // In dieser Fixture wurde nie scharfgestellt; deshalb gibt es keine gemessene Aufnahmedauer.
        Assert.Equal(0, report.ArmedToOutcomeMs);
        Assert.Equal(0, report.PeakAmplitude);
        Assert.Equal(nameof(InvalidOperationException), report.FaultKind);
    }

    [Fact]
    public async Task SuccessfulOutcomes_CarryNoFaultKind()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        await trace.RecordNoSpeechAsync();

        Assert.Null(Assert.Single(reporter.Reports).FaultKind);
    }

    [Fact]
    public async Task AnArmedAttempt_CarriesItsTurnId_SoTheReportJoinsTheRecognition()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        await trace.RecordNoSpeechAsync();

        var turnId = Assert.Single(reporter.Reports).TurnId;
        Assert.True(
            Guid.TryParseExact(turnId, "N", out _),
            $"expected a new per-turn id, got: {turnId ?? "<null>"}");
    }

    [Fact]
    public async Task EveryArming_MintsANewTurnId()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        await trace.RecordNoSpeechAsync();
        await trace.RecordArmedAsync();
        await trace.RecordNoSpeechAsync();

        Assert.Equal(2, reporter.Reports.Count);
        Assert.NotEqual(reporter.Reports[0].TurnId, reporter.Reports[1].TurnId);
    }

    [Fact]
    public async Task AFaultBeforeArming_CarriesNoTurnId_InsteadOfBorrowingThePreviousTurns()
    {
        var reporter = new CapturingReporter();
        var trace = new ActiveCaptureTrace(new SilentDebugConsole(), NullLogger.Instance, reporter);

        await trace.RecordArmedAsync();
        await trace.RecordNoSpeechAsync();

        // Die wiederverwendete Spur darf einen Fehler vor dem nächsten Scharfstellen
        // nicht über die alte Kennung mit dem vorherigen Versuch verbinden.
        await trace.RecordFailureAsync(new InvalidOperationException("mic gate"));

        Assert.Equal(2, reporter.Reports.Count);
        Assert.NotNull(reporter.Reports[0].TurnId);
        Assert.Null(reporter.Reports[1].TurnId);
    }
}
