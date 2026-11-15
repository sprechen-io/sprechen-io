namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpellingConfidenceEscalationPolicyTests
{
    // Die internen Aufzählungen werden in den öffentlichen Testparametern
    // als Zahlen übergeben und im Test zurückgewandelt.
    private const int Sufficient = (int)SttConfidenceLevel.Sufficient;
    private const int Uncertain = (int)SttConfidenceLevel.Uncertain;
    private const int Insufficient = (int)SttConfidenceLevel.Insufficient;
    private const int Escalate = (int)SpellingEscalationDecision.Escalate;
    private const int Defer = (int)SpellingEscalationDecision.Defer;

    [Theory]
    [InlineData(InputMode.Spelling, Insufficient, 0, Escalate)]
    [InlineData(InputMode.Spelling, Insufficient, 40, Escalate)]
    [InlineData(InputMode.Spelling, Insufficient, 64, Escalate)]
    [InlineData(InputMode.Spelling, Uncertain, 70, Defer)]
    [InlineData(InputMode.Spelling, Sufficient, 99, Defer)]
    // Ohne gemeldete Konfidenz greift dieser zusätzliche Buchstabierzweig nicht.
    [InlineData(InputMode.Spelling, Insufficient, null, Defer)]
    [InlineData(InputMode.Voice, Insufficient, 40, Defer)]
    [InlineData(InputMode.Digits, Insufficient, 40, Defer)]
    [InlineData(InputMode.Keyboard, Insufficient, 40, Defer)]
    [InlineData(InputMode.Literal, Insufficient, 40, Defer)]
    public void Decide_ReturnsCategoricalDecisionForBoundaryAndNegativeRows(
        InputMode expectedInputMode,
        int confidenceLevel,
        int? sttConfidencePercent,
        int expected)
    {
        var policy = new SpellingConfidenceEscalationPolicy();

        var decision = policy.Decide(expectedInputMode, (SttConfidenceLevel)confidenceLevel, sttConfidencePercent);

        Assert.Equal((SpellingEscalationDecision)expected, decision);
    }

    [Fact]
    public void Decide_IsDeterministic_ForRepeatedIdenticalInputs()
    {
        var policy = new SpellingConfidenceEscalationPolicy();

        var first = policy.Decide(InputMode.Spelling, SttConfidenceLevel.Insufficient, 30);
        var second = policy.Decide(InputMode.Spelling, SttConfidenceLevel.Insufficient, 30);

        Assert.Equal(SpellingEscalationDecision.Escalate, first);
        Assert.Equal(first, second);
    }
}
