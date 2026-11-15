namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantIdleReducerTests
{
    [Fact]
    public void Reduce_WhenStudySessionBound_StoresSessionAndEmitsNoEffects()
    {
        var session = new StudySession("study.umfrage", "hintergrund-und-setup");
        var state = SpeechAssistantState.Initial;

        var update = SpeechAssistantIdleReducer.Reduce(
            state,
            new SpeechAssistantStudySessionBoundEvent(session));

        Assert.Equal(session, update.State.StudySession);
        Assert.Equal(SpeechAssistantPhase.Idle, update.State.Phase);
        Assert.Empty(update.Effects);
    }
}
