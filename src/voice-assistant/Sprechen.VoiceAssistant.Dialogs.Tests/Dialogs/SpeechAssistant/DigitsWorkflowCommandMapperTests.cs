namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class DigitsWorkflowCommandMapperTests
{
    [Theory]
    [InlineData(DigitsWorkflowCommandIds.DeleteLastDigit, DigitsWorkflowCommand.RemoveLastDigit)]
    [InlineData(DigitsWorkflowCommandIds.DeleteLastBlock, DigitsWorkflowCommand.RemoveLastBlock)]
    [InlineData(DigitsWorkflowCommandIds.RepeatLastBlock, DigitsWorkflowCommand.ReadbackDigits)]
    [InlineData(DigitsWorkflowCommandIds.RedictateLastBlock, DigitsWorkflowCommand.RedictateLastBlock)]
    public void LocalCommandMapper_MapsDigitsWorkflowCommands(
        string commandId,
        DigitsWorkflowCommand expectedCommand)
    {
        var success = new DigitsWorkflowLocalCommandMapper().TryMap(commandId, out var command);

        Assert.True(success);
        Assert.Equal(expectedCommand, command);
    }

    [Fact]
    public void LocalCommandMapper_TaughtRepeatPhrase_IsNonDestructiveReadback()
    {
        new DigitsWorkflowLocalCommandMapper().TryMap(DigitsWorkflowCommandIds.RepeatLastBlock, out var command);

        Assert.Equal(DigitsWorkflowCommand.ReadbackDigits, command);
        Assert.NotEqual(DigitsWorkflowCommand.RedictateLastBlock, command);
    }

    [Theory]
    [InlineData(GlobalCommandIds.Next, DigitsWorkflowCommand.Continue)]
    [InlineData(GlobalCommandIds.Repeat, DigitsWorkflowCommand.ReadbackDigits)]
    // Der globale Abschlussbefehl wird innerhalb der Zifferneingabe auf Continue abgebildet.
    // Damit kann das angebotene Abschlusswort die Eingabe beenden.
    [InlineData(GlobalCommandIds.Submit, DigitsWorkflowCommand.Continue)]

    [InlineData(GlobalCommandIds.AcceptValue, DigitsWorkflowCommand.Continue)]
    public void GlobalCommandMapper_MapsSupportedGlobalCommands(
        string commandId,
        DigitsWorkflowCommand expectedCommand)
    {
        var success = new DigitsWorkflowGlobalCommandMapper().TryMap(commandId, out var command);

        Assert.True(success);
        Assert.Equal(expectedCommand, command);
    }

    [Fact]
    public void GlobalCommandMapper_Repeat_NeverReachesDestructiveReDictation()
    {
        new DigitsWorkflowGlobalCommandMapper().TryMap(GlobalCommandIds.Repeat, out var command);

        Assert.Equal(DigitsWorkflowCommand.ReadbackDigits, command);
        Assert.NotEqual(DigitsWorkflowCommand.RedictateLastBlock, command);
    }

    [Fact]
    public void GlobalCommandMapper_Back_IsNoLongerADigitsLocalEdit()
    {
        var success = new DigitsWorkflowGlobalCommandMapper().TryMap(GlobalCommandIds.Back, out var command);

        Assert.False(success);
        Assert.Equal(DigitsWorkflowCommand.None, command);
    }
}
