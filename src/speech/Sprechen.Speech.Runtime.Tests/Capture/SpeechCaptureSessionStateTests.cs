using Moq;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class SpeechCaptureSessionStateTests
{
    [Fact]
    public void TryGetWarmSession_WhenSessionIsRunning_ReturnsMimeTypeAndCompletion()
    {
        var state = new SpeechCaptureSessionState();
        using var sessionCts = new CancellationTokenSource();
        var recording = new Mock<IMicrophoneRecording>(MockBehavior.Strict);
        var recordingCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        recording.SetupGet(candidate => candidate.Completion).Returns(recordingCompletion.Task);
        var sessionTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        state.Start(
            sessionCts,
            recording.Object,
            SpeechAudioMimeType.Pcm,
            sessionTask.Task,
            sessionTask.Task,
            recordingCompletion.Task);

        var result = state.TryGetWarmSession(out var session);

        Assert.True(result);
        Assert.Equal(SpeechAudioMimeType.Pcm, session.MimeType);
        Assert.Same(sessionTask.Task, session.Completion);
        Assert.Same(recordingCompletion.Task, session.RecordingCompletion);
    }

    [Fact]
    public void Clear_WhenSessionExists_ReturnsSnapshotAndResetsWarmSession()
    {
        var state = new SpeechCaptureSessionState();
        using var sessionCts = new CancellationTokenSource();
        var recording = new Mock<IMicrophoneRecording>(MockBehavior.Strict);
        recording.SetupGet(candidate => candidate.Completion).Returns(Task.CompletedTask);
        var sessionTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        state.Start(
            sessionCts,
            recording.Object,
            SpeechAudioMimeType.Pcm,
            sessionTask.Task,
            sessionTask.Task,
            Task.CompletedTask);

        var snapshot = state.Clear();

        Assert.True(snapshot.HasSession);
        Assert.Same(sessionTask.Task, snapshot.SessionTask);
        Assert.Same(sessionCts, snapshot.SessionCts);
        Assert.Same(recording.Object, snapshot.Recording);
        Assert.False(state.TryGetWarmSession(out _));
    }

    [Fact]
    public void TryGetWarmSession_WhenRecordingEndedButPumpStillRuns_ReturnsFalse()
    {
        var state = new SpeechCaptureSessionState();
        using var sessionCts = new CancellationTokenSource();
        var recording = new Mock<IMicrophoneRecording>(MockBehavior.Strict);
        var recordingCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        recording.SetupGet(candidate => candidate.Completion).Returns(recordingCompletion.Task);
        var sessionCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        state.Start(
            sessionCts,
            recording.Object,
            SpeechAudioMimeType.Pcm,
            sessionCompletion.Task,
            sessionCompletion.Task,
            recordingCompletion.Task);
        recordingCompletion.TrySetResult();

        Assert.False(state.TryGetWarmSession(out _));
        Assert.False(sessionCompletion.Task.IsCompleted);
    }
}
