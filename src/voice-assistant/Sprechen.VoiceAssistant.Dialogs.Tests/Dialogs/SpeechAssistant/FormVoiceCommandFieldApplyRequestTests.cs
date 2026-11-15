namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFieldApplyRequestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenTranscriptForApplyIsMissing_Throws(string? transcriptForApply)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FormVoiceCommandFieldApplyRequest(
            InputMode.Voice,
            transcriptForApply!,
            ambiguityTelemetry: null,
            sttConfidencePercent: null));
    }

    [Fact]
    public void ForEmptySetSelection_CarriesTheCanonicalEmptyWireFormWithoutThrowing()
    {
        // Für eine ausdrücklich leere Mehrfachauswahl einen Auftrag mit leerem
        // kanonischem Wert zulassen. Die spätere Validierung, Übernahme und
        // Bestätigung werden durch diesen Konstruktionstest nicht geprüft.
        var request = FormVoiceCommandFieldApplyRequest.ForEmptySetSelection(InputMode.Voice, sttConfidencePercent: 97);

        Assert.Equal(InputMode.Voice, request.ExpectedInputMode);
        Assert.Equal(string.Empty, request.TranscriptForApply);
        Assert.Null(request.AmbiguityTelemetry);
        Assert.Equal(97, request.SttConfidencePercent);

    }

    [Fact]
    public void Constructor_WhenValid_PreservesApplyFacts()
    {
        var telemetry = new AmbiguityTelemetryData(
            "input_vs_command",
            "context_default",
            CommandSignalClass.Clear,
            "accepted",
            GlobalCommandIds.Next);

        var request = new FormVoiceCommandFieldApplyRequest(InputMode.Voice, "Alice", telemetry, 88);

        Assert.Equal(InputMode.Voice, request.ExpectedInputMode);
        Assert.Equal("Alice", request.TranscriptForApply);
        Assert.Equal(telemetry, request.AmbiguityTelemetry);
        Assert.Equal(88, request.SttConfidencePercent);
    }
}
