using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandNoInputFeedbackObserverTests
{
    [Fact]
    public async Task OnAttemptStarting_FirstAttempt_DoesNotSpeak()
    {
        var tts = new RecordingTextToSpeechPlayer();
        var observer = new FormVoiceCommandNoInputFeedbackObserver(tts);
        observer.SetActiveCapture("de", speechRate: 1.0d, PolitenessLevel.Formal);

        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(1, 3, FormVoiceCommandAttemptFailure.None), CancellationToken.None);

        Assert.Empty(tts.Calls);
    }

    [Fact]
    public async Task OnAttemptStarting_EachElapsedNoInputWindow_SpeaksTheNoInputCue()
    {
        var tts = new RecordingTextToSpeechPlayer();
        var observer = new FormVoiceCommandNoInputFeedbackObserver(tts);
        observer.SetActiveCapture("de", speechRate: 1.0d, PolitenessLevel.Formal);

        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(1, 3, FormVoiceCommandAttemptFailure.None), CancellationToken.None);
        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.NoInputTimeout), CancellationToken.None);
        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(3, 3, FormVoiceCommandAttemptFailure.NoInputTimeout), CancellationToken.None);

        var expected = NoInputCapturePromptTextResolver.GetNoInputPrompt("de");
        Assert.Equal(2, tts.Calls.Count);
        Assert.All(tts.Calls, call => Assert.Equal(expected, call.Text));
        Assert.All(tts.Calls, call => Assert.Equal("de", call.LanguageCode));

        // Die Kennzeichnung NoInput erlaubt die Zuordnung der Ansage unabhängig vom Wortlaut.
        Assert.All(tts.AnnouncementKinds, kind => Assert.Equal(SpokenAnnouncementKind.NoInput, kind));
    }

    [Fact]
    public async Task OnAttemptStarting_InformalCapture_SpeaksTheInformalNoInputCue()
    {
        var tts = new RecordingTextToSpeechPlayer();
        var observer = new FormVoiceCommandNoInputFeedbackObserver(tts);
        observer.SetActiveCapture("de", speechRate: 1.0d, PolitenessLevel.Informal);

        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.NoInputTimeout), CancellationToken.None);

        Assert.Equal("Ich habe nichts gehört. Bitte sprich noch einmal.", Assert.Single(tts.Calls).Text);
    }

    [Fact]
    public async Task OnAttemptStarting_UnrecognizedAnswerWindow_DoesNotSpeak()
    {
        var tts = new RecordingTextToSpeechPlayer();
        var observer = new FormVoiceCommandNoInputFeedbackObserver(tts);
        observer.SetActiveCapture("de", speechRate: 1.0d, PolitenessLevel.Formal);

        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.UnrecognizedAnswer), CancellationToken.None);

        Assert.Empty(tts.Calls);
    }

    [Fact]
    public async Task OnAttemptStarting_WithoutActiveCapture_DoesNotSpeak()
    {
        var tts = new RecordingTextToSpeechPlayer();
        var observer = new FormVoiceCommandNoInputFeedbackObserver(tts);

        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.NoInputTimeout), CancellationToken.None);

        Assert.Empty(tts.Calls);
    }

    [Fact]
    public async Task OnAttemptStarting_AfterClearActiveCapture_DoesNotSpeak()
    {
        var tts = new RecordingTextToSpeechPlayer();
        var observer = new FormVoiceCommandNoInputFeedbackObserver(tts);
        observer.SetActiveCapture("de", speechRate: 1.0d, PolitenessLevel.Formal);
        observer.ClearActiveCapture();

        await observer.OnAttemptStartingAsync(new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.NoInputTimeout), CancellationToken.None);

        Assert.Empty(tts.Calls);
    }
}
