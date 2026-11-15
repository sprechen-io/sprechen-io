namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeWordMissTelemetryActionFactoryTests
{
    private readonly WakeWordMissTelemetryActionFactory _factory = new(
        new WakeWordTelemetryValueFormatter(),
        SpeechAssistantTraceScope.WakeWordEffect,
        SpeechAssistantTraceScope.PostWakeWordRecovery);

    [Fact]
    public void MissTrace_NoRecognizedSpeech_CreatesInformationTrace()
    {
        var action = _factory.Create(new WakeWordMissEvent(
            "corr",
            WakeWordMissPhase.MissTrace,
            WakeWordActivationStatus.NoRecognizedSpeech,
            null,
            null,
            null,
            null,
            null,
            null));

        Assert.Null(action.LogRecord);
        var traceRecord = AssertTraceRecord(action);
        Assert.Equal(SpeechAssistantTraceScope.WakeWordEffect, traceRecord.Scope);
        Assert.Equal(SpeechAssistantTraceLevel.Information, traceRecord.Level);
        Assert.Equal("Wake-word transcription returned no recognized speech.", traceRecord.Message);
    }

    [Fact]
    public void MissTrace_NonMatchingInventory_CreatesWarningTraceWithConfidence()
    {
        var action = _factory.Create(new WakeWordMissEvent(
            "corr",
            WakeWordMissPhase.MissTrace,
            WakeWordActivationStatus.MissingLeadingWakeWord,
            42,
            null,
            null,
            null,
            null,
            null));

        var traceRecord = AssertTraceRecord(action);
        Assert.Equal(SpeechAssistantTraceLevel.Warning, traceRecord.Level);
        Assert.Equal("Wake-word transcription did not match the modeled inventory. confidence=42", traceRecord.Message);
    }

    [Fact]
    public void MissDiagnostic_CreatesPostWakeRecoveryTrace()
    {
        var action = _factory.Create(new WakeWordMissEvent(
            "corr",
            WakeWordMissPhase.MissDiagnostic,
            WakeWordActivationStatus.MissingLeadingWakeWord,
            33,
            8,
            4,
            2,
            null,
            new WakeWordMissDiagnosticDetail(
                TokenCount: 1,
                ActivationForms: new[] { "Okay" },
                RecognitionOnlyActivationForms: new[] { "Ok" })));

        var traceRecord = AssertTraceRecord(action);
        Assert.Equal(SpeechAssistantTraceScope.PostWakeWordRecovery, traceRecord.Scope);
        Assert.Equal("wake miss status=MissingLeadingWakeWord transcriptLen=8 confidence=33 activationFormCount=4 recognitionOnlyActivationFormCount=2 tokenCount=1 activationForms=[Okay] recognitionOnlyActivationForms=[Ok]", traceRecord.Message);
    }

    [Fact]
    public void MissDiagnostic_DoesNotRenderParticipantSpeechInTrace()
    {
        const string participantSpeech = "de89 3704 0044 0532 0130 00";
        var action = _factory.Create(new WakeWordMissEvent(
            "corr",
            WakeWordMissPhase.MissDiagnostic,
            WakeWordActivationStatus.MissingLeadingWakeWord,
            97,
            participantSpeech.Length,
            2,
            1,
            null,
            new WakeWordMissDiagnosticDetail(
                TokenCount: 6,
                ActivationForms: new[] { "Okay Alex" },
                RecognitionOnlyActivationForms: new[] { "Ok Alex" })));

        var traceRecord = AssertTraceRecord(action);

        Assert.DoesNotContain(participantSpeech, traceRecord.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("normalizedTranscript", traceRecord.Message, StringComparison.Ordinal);
        Assert.Contains($"transcriptLen={participantSpeech.Length}", traceRecord.Message, StringComparison.Ordinal);
        Assert.Contains("tokenCount=6", traceRecord.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailurePhase_CreatesDebugLogAndWarningTrace()
    {
        var exception = new InvalidOperationException("boom");

        var action = _factory.Create(new WakeWordMissEvent(
            "corr",
            WakeWordMissPhase.EarconFailed,
            WakeWordActivationStatus.MissingLeadingWakeWord,
            null,
            null,
            null,
            null,
            exception,
            null));

        var logRecord = AssertLogRecord(action);
        Assert.Same(exception, logRecord.Exception);
        Assert.Equal("Wake-word miss earcon playback failed.", logRecord.Message);

        var traceRecord = AssertTraceRecord(action);
        Assert.Equal(SpeechAssistantTraceScope.WakeWordEffect, traceRecord.Scope);
        Assert.Equal(SpeechAssistantTraceLevel.Warning, traceRecord.Level);
        Assert.Equal("Wake-word miss earcon playback failed; continuing listener loop.", traceRecord.Message);
    }

    [Fact]
    public void UnknownPhase_CreatesNoActionForCompatibilityOnlyFallback()
    {
        var action = _factory.Create(new WakeWordMissEvent(
            "corr",
            (WakeWordMissPhase)999,
            WakeWordActivationStatus.MissingLeadingWakeWord,
            null,
            null,
            null,
            null,
            null,
            null));

        Assert.Null(action.TraceRecord);
        Assert.Null(action.LogRecord);
    }

    private static WakeWordTelemetryTraceRecord AssertTraceRecord(WakeWordMissTelemetryAction action)
    {
        Assert.True(action.TraceRecord.HasValue);
        return action.TraceRecord.Value;
    }

    private static WakeWordTelemetryLogRecord AssertLogRecord(WakeWordMissTelemetryAction action)
    {
        Assert.True(action.LogRecord.HasValue);
        return action.LogRecord.Value;
    }
}
