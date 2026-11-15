namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionCaptureFailureTransitionPolicyTests
{
    [Fact]
    public void Constructor_RejectsNegativeRetryBudget()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectionCaptureFailureTransitionPolicy(-1));
    }

    [Fact]
    public void Evaluate_WhenRetryBudgetIsZero_TerminatesOnFirstFailure()
    {
        var policy = new SelectionCaptureFailureTransitionPolicy(maximumRetries: 0);

        var decision = policy.Evaluate(
            "SPEECH_CAPTURE_INTERRUPTED",
            currentFailureCount: 0,
            SelectionDialogPhase.Listening);

        Assert.Equal(SelectionCaptureFailureTransitionKind.Terminal, decision.Kind);
        Assert.Equal(0, decision.ExpectedCurrentCount);
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Listening)]
    [InlineData(SelectionDialogPhase.Confirming)]
    public void Evaluate_UsesOneSharedInclusiveFailureBoundary(SelectionDialogPhase phase)
    {
        var policy = new SelectionCaptureFailureTransitionPolicy(maximumRetries: 2);

        var first = policy.Evaluate("SPEECH_CAPTURE_INTERRUPTED", 0, phase);
        var second = policy.Evaluate("SPEECH_CAPTURE_INTERRUPTED", 1, phase);

        Assert.Equal(SelectionCaptureFailureTransitionKind.Retry, first.Kind);
        Assert.Equal(SelectionCaptureFailureTransitionKind.Terminal, second.Kind);
    }

    [Theory]
    [InlineData("STT_QUEUE_FULL")]
    [InlineData("STT_BACKOFF_EXHAUSTED")]
    [InlineData("SPEECH_CAPTURE_INTERRUPTED")]
    public void Evaluate_AcceptsEveryFailureCodeThatHasARecordedAttribution(string failureCode)
    {
        var policy = new SelectionCaptureFailureTransitionPolicy(maximumRetries: 2);

        var decision = policy.Evaluate(failureCode, 0, SelectionDialogPhase.Listening);

        Assert.Equal(SelectionCaptureFailureTransitionKind.Retry, decision.Kind);
    }

    [Fact]
    public void Evaluate_WhenTheFailureCodeHasNoRecordedAttribution_RefusesToSpendTheBudget()
    {
        // Unbekannte Fehlercodes dürfen keine Wiederholungsversuche verbrauchen.
        // Ihre Behandlung muss zuerst ausdrücklich festgelegt werden.
        var policy = new SelectionCaptureFailureTransitionPolicy(maximumRetries: 2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => policy.Evaluate("A_CODE_NOBODY_CLASSIFIED", 0, SelectionDialogPhase.Listening));
    }
}
