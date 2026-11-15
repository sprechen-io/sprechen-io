namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zuordnung globaler Befehle zum Abschluss oder Vorlesen im Buchstabierablauf.
/// Andere Befehle werden von dieser Zuordnung nicht behandelt.
/// </summary>
public sealed class SpellingWorkflowGlobalCommandMapperTests
{
    [Theory]
    [InlineData(GlobalCommandIds.Submit, SpellingWorkflowCommand.Complete)]
    [InlineData(GlobalCommandIds.AcceptValue, SpellingWorkflowCommand.Complete)]
    [InlineData(GlobalCommandIds.InputModeVoice, SpellingWorkflowCommand.Complete)]
    [InlineData(GlobalCommandIds.Next, SpellingWorkflowCommand.Complete)]
    [InlineData(GlobalCommandIds.Repeat, SpellingWorkflowCommand.ReadbackCharacters)]
    public void GlobalCommandMapper_MapsDoneEquivalentsAndReadback(string commandId, SpellingWorkflowCommand expected)
    {
        var success = new SpellingWorkflowGlobalCommandMapper().TryMap(commandId, out var command);

        Assert.True(success);
        Assert.Equal(expected, command);
    }

    [Theory]
    [InlineData(GlobalCommandIds.Help)]
    [InlineData(GlobalCommandIds.JumpTo)]
    [InlineData(GlobalCommandIds.EditValue)]
    public void GlobalCommandMapper_LeavesOtherGlobalCommandsUnmapped(string commandId)
    {
        var success = new SpellingWorkflowGlobalCommandMapper().TryMap(commandId, out var command);

        Assert.False(success);
        Assert.Equal(SpellingWorkflowCommand.None, command);
    }
}
