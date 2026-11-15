using Moq;
using Sprechen.Speech.ModelRuntime.PromptTemplates;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft nach bestätigter Übernahme die Verzweigung zwischen automatischem
/// Fokuswechsel und manuellem Fortsetzungshinweis. Gemessen werden Aufrufe
/// und angeforderte Ansagetexte; tatsächliche Fokusereignisse und hörbare
/// Wiedergabe liegen außerhalb dieser Tests.
/// </summary>
public sealed class FormVoiceCommandConfirmationAdvanceRegressionTests
{
    [Fact]
    public async Task AcceptedConfirmation_WithAutoAdvance_AttemptsFocusAdvance_AndSpeaksCommitAccepted()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture("übernehmen") { AutoAdvanceAfterCommit = true };
        fixture.Interpreter
            .Setup(i => i.TryMoveFocusNextAsync())
            .ReturnsAsync(true);
        fixture.Interpreter
            .Setup(i => i.GetFocusedFieldAsync())
            .ReturnsAsync(FormVoiceCommandConfirmationReaskFixture.Field("quizkategorien_field", "kategorien"));

        var result = await fixture.HandleAsync();

        Assert.True(result.ShouldContinue);
        fixture.Interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Once);
        Assert.Contains(
            FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de"),
            fixture.Spoken);
        Assert.DoesNotContain(
            FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de"),
            fixture.Spoken);
        Assert.DoesNotContain(
            FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationShortPrompt("de"),
            fixture.Spoken);
    }

    [Fact]
    public async Task AcceptedConfirmation_WithoutAutoAdvance_SpeaksManualContinuation_AndNeverMovesFocus()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture("übernehmen");

        var result = await fixture.HandleAsync();

        Assert.True(result.ShouldContinue);
        fixture.Interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Never);
        Assert.Contains(
            FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de"),
            fixture.Spoken);
        Assert.DoesNotContain(
            FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt("de"),
            fixture.Spoken);
    }
}
