namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft, wann Aufnahmebeobachtungen zurückgesetzt werden und welche Werte
/// zwischen Versuchen erhalten bleiben.
/// </summary>
public sealed class ActiveCaptureAttemptObservationStateTests
{
    [Fact]
    public void MarkArmed_ClearsTheFrameAndLevelObservations_AndKeepsTheCursorHandoff()
    {
        var state = new ActiveCaptureAttemptObservationState();
        state.MarkStarted(SpeechCaptureMode.Listening, SpeechCaptureStartContext.CleanListening);
        state.MarkCursorHandoff(startCursorHandedOff: true);
        state.ObserveLookbackBytes(640);
        state.ObservePeakAmplitude(900);
        state.ObserveOnsetDecision(estimatedFloor: 40, appliedOnsetThreshold: 90);
        state.ObserveArrivedFrames(12);

        state.MarkArmed();

        Assert.Equal(0, state.PeakAmplitude);
        Assert.Equal(0, state.EstimatedFloor);
        Assert.Equal(0, state.AppliedOnsetThreshold);
        Assert.Equal(0, state.ObservedFrames);
        var window = state.ToWindowObservation();
        Assert.True(window.StartCursorHandedOff);
        Assert.Equal(640, window.LookbackBytes);
    }

    [Fact]
    public void MarkArmed_LeavesTheAttemptIdentityAlone()
    {
        var state = new ActiveCaptureAttemptObservationState();
        state.MarkStarted(SpeechCaptureMode.ShortConfirmation, SpeechCaptureStartContext.AssistantPlaybackActive);

        state.MarkArmed();

        Assert.Equal(SpeechCaptureMode.ShortConfirmation, state.CaptureMode);
        Assert.Equal(SpeechCaptureStartContext.AssistantPlaybackActive, state.StartContext);
    }

    [Fact]
    public void MarkCursorHandoff_SetsTheHandoffAndClearsTheLookback()
    {
        var state = new ActiveCaptureAttemptObservationState();
        state.ObserveLookbackBytes(1280);

        state.MarkCursorHandoff(startCursorHandedOff: false);

        var window = state.ToWindowObservation();
        Assert.False(window.StartCursorHandedOff);
        Assert.Equal(0, window.LookbackBytes);
    }

    [Fact]
    public void MarkStarted_StopsTheClockOfThePreviousAttempt()
    {
        var state = new ActiveCaptureAttemptObservationState();
        state.MarkStarted(SpeechCaptureMode.Listening, SpeechCaptureStartContext.CleanListening);
        state.MarkArmed();
        Assert.True(state.ResolveArmedToOutcomeMs() >= 0);

        state.MarkStarted(SpeechCaptureMode.PromptInterruption, SpeechCaptureStartContext.AssistantPlaybackActive);

        Assert.Equal(0, state.ResolveArmedToOutcomeMs());
        Assert.Equal(SpeechCaptureMode.PromptInterruption, state.CaptureMode);
    }

    [Fact]
    public void ObservationsSurviveAnAttemptThatNeverArms()
    {
        var state = new ActiveCaptureAttemptObservationState();
        state.MarkStarted(SpeechCaptureMode.Listening, SpeechCaptureStartContext.CleanListening);
        state.MarkArmed();
        state.ObservePeakAmplitude(700);
        state.ObserveArrivedFrames(9);

        // Dokumentiertes Verhalten: ohne Scharfstellen traegt der Folgeversuch die Werte des
        // Vorversuchs weiter, weil die Spur am Koordinator haengt und nicht am Versuch.
        state.MarkStarted(SpeechCaptureMode.Listening, SpeechCaptureStartContext.CleanListening);

        Assert.Equal(700, state.PeakAmplitude);
        Assert.Equal(9, state.ObservedFrames);
    }

    [Fact]
    public void PeakAndFrameCountAreMaxima()
    {
        var state = new ActiveCaptureAttemptObservationState();
        state.ObservePeakAmplitude(500);
        state.ObserveArrivedFrames(20);

        state.ObservePeakAmplitude(10);
        state.ObserveArrivedFrames(3);

        Assert.Equal(500, state.PeakAmplitude);
        Assert.Equal(20, state.ObservedFrames);
    }

    [Theory]
    [InlineData(0, 0, "no_speech")]
    [InlineData(4, 0, "digital_silence")]
    [InlineData(4, 7, "no_speech")]
    public void ResolveNoSpeechOutcome_SeparatesDigitalSilenceFromSilence(int frames, int peak, string expected)
    {
        var state = new ActiveCaptureAttemptObservationState();
        state.ObserveArrivedFrames(frames);
        state.ObservePeakAmplitude(peak);

        Assert.Equal(expected, state.ResolveNoSpeechOutcome());
    }
}
