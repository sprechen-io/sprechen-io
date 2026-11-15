namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandTelemetryProjectionTests
{
    [Theory]
    [InlineData("deterministic", InputVsCommandResolutionStrategyMapper.ContextDefault)]
    [InlineData("none", InputVsCommandResolutionStrategyMapper.ContextDefault)]
    [InlineData("prefix", "prefix")]
    [InlineData("spell_fallback", "spell_fallback")]
    [InlineData("query", "query")]
    [InlineData("unknown", InputVsCommandResolutionStrategyMapper.ContextDefault)]
    [InlineData(null, InputVsCommandResolutionStrategyMapper.ContextDefault)]
    public void ResolutionStrategyMapper_MapsKnownStrategiesAndDefaultsUnknown(
        string? strategy,
        string expected)
    {
        var mapped = new InputVsCommandResolutionStrategyMapper().Map(strategy);

        Assert.Equal(expected, mapped);
    }

    [Theory]
    [InlineData(DialogMicroStateIds.Prompting, DialogStates.Prompting)]
    [InlineData(DialogMicroStateIds.Listening, DialogStates.Listening)]
    [InlineData(DialogMicroStateIds.Interpreting, DialogStates.Interpreting)]
    [InlineData(DialogMicroStateIds.Validating, DialogStates.Validating)]
    [InlineData(DialogMicroStateIds.Confirmation, DialogStates.Confirmation)]
    [InlineData(DialogMicroStateIds.ErrorPrompt, DialogStates.ErrorPrompt)]
    public void DialogStateProjection_MapsSemanticMicroStatesToTelemetryStates(
        string microState,
        string expectedTelemetryState)
    {
        var success = DialogStateTelemetryProjection.TryProjectMicroStateToTelemetryState(microState, out var telemetryState);

        Assert.True(success);
        Assert.Equal(expectedTelemetryState, telemetryState);
    }

    [Theory]
    [InlineData(DialogStates.Init)]
    [InlineData(DialogStates.Prompting)]
    [InlineData(DialogStates.Listening)]
    [InlineData(DialogStates.Interpreting)]
    [InlineData(DialogStates.Validating)]
    [InlineData(DialogStates.Confirmation)]
    [InlineData(DialogStates.ErrorPrompt)]
    [InlineData(DialogStates.CancelConfirmation)]
    public void DialogTelemetryStateCatalog_AcceptsKnownTelemetryStates(string telemetryState)
    {
        Assert.True(DialogStateTelemetryProjection.IsKnownTelemetryState(telemetryState));
    }
}
