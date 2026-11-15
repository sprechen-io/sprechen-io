using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Pufferänderungen, Abschlussentscheidung und angeforderte Texte
/// für Korrektur-, Abschluss- und Vorlesebefehle beim Buchstabieren.
/// </summary>
public sealed class SpellingWorkflowCommandActionFlowTests
{
    [Fact]
    public async Task HandleAsync_Complete_NonEmptyBuffer_Finalizes()
    {
        var spoken = new List<string>();
        var flow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("ab", "c");

        var outcome = await flow.HandleAsync(
            SpellingWorkflowCommand.Complete,
            buffer,
            CreateBlockReadback(spoken, spellOutAllowed: true),
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.True(outcome.Finalized);
        Assert.Empty(spoken);
    }

    [Fact]
    public async Task HandleAsync_Complete_EmptyBuffer_SpeaksNoValue_AndWaits()
    {
        var spoken = new List<string>();
        var flow = CreateActionFlow(spoken);
        var buffer = new SpellingWorkflowBuffer();

        var outcome = await flow.HandleAsync(
            SpellingWorkflowCommand.Complete,
            buffer,
            CreateBlockReadback(spoken, spellOutAllowed: true),
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.False(outcome.Finalized);
        Assert.Single(spoken);
    }

    [Theory]
    [InlineData(SpellingWorkflowCommand.RemoveLastCharacter, "a")]
    [InlineData(SpellingWorkflowCommand.RemoveLastBlock, "a")]
    [InlineData(SpellingWorkflowCommand.ClearBuffer, "")]
    public async Task HandleAsync_EditCommand_MutatesBuffer_AndSpeaks(SpellingWorkflowCommand command, string expected)
    {
        var spoken = new List<string>();
        var flow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("a", "b");

        var outcome = await flow.HandleAsync(
            command,
            buffer,
            CreateBlockReadback(spoken, spellOutAllowed: true),
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.False(outcome.Finalized);
        Assert.Equal(expected, buffer.GetValue());
        Assert.Single(spoken);
        Assert.NotEqual(string.Empty, spoken[0].Trim());
    }

    [Fact]
    public async Task HandleAsync_EditCommand_EmptyBuffer_SpeaksNoValue()
    {
        var spoken = new List<string>();
        var flow = CreateActionFlow(spoken);
        var buffer = new SpellingWorkflowBuffer();

        await flow.HandleAsync(
            SpellingWorkflowCommand.RemoveLastBlock,
            buffer,
            CreateBlockReadback(spoken, spellOutAllowed: true),
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.Equal(string.Empty, buffer.GetValue());
        Assert.Single(spoken);
    }

    [Fact]
    public async Task HandleAsync_ReadbackCharacters_NonEmpty_SpeaksReadback_DoesNotMutate()
    {
        var spoken = new List<string>();
        var flow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("ab", "c");

        await flow.HandleAsync(
            SpellingWorkflowCommand.ReadbackCharacters,
            buffer,
            CreateBlockReadback(spoken, spellOutAllowed: true),
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.Equal("abc", buffer.GetValue());
        Assert.Single(spoken);
        Assert.Contains("PHON[abc]", spoken[0], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SpellingWorkflowCommandIds.Readback, false)]
    [InlineData(GlobalCommandIds.Repeat, true)]
    public async Task HandleAsync_ReadbackCharacters_WhenProtectedFromLocalOrGlobalCommand_SpeaksGuidanceAndWaits(
        string commandId,
        bool isGlobal)
    {
        var spoken = new List<string>();
        var flow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("ab", "c");
        SpellingWorkflowCommand command;
        var mapped = isGlobal
            ? new SpellingWorkflowGlobalCommandMapper().TryMap(commandId, out command)
            : new SpellingWorkflowLocalCommandMapper().TryMap(commandId, out command);

        var outcome = await flow.HandleAsync(
            command,
            buffer,
            CreateBlockReadback(spoken, spellOutAllowed: false),
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.True(mapped);
        Assert.False(outcome.Finalized);
        Assert.Equal("abc", buffer.GetValue());
        Assert.Equal(
            "Aus Datenschutzgr\u00fcnden kann ich diesen Wert nicht vorlesen. Sagen Sie die n\u00e4chsten Buchstaben, oder fertig.",
            Assert.Single(spoken));
    }

    [Fact]
    public async Task HandleAsync_ReadbackCharacters_EmptyBuffer_SpeaksNoValue()
    {
        var spoken = new List<string>();
        var flow = CreateActionFlow(spoken);
        var buffer = new SpellingWorkflowBuffer();

        await flow.HandleAsync(
            SpellingWorkflowCommand.ReadbackCharacters,
            buffer,
            CreateBlockReadback(spoken, spellOutAllowed: true),
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.Single(spoken);
        Assert.DoesNotContain("PHON[", spoken[0], StringComparison.Ordinal);
    }

    private static SpellingWorkflowCommandActionFlow CreateActionFlow(List<string> spoken)
    {
        var speech = CreateSpeech(spoken);
        return new SpellingWorkflowCommandActionFlow(
            new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose).Object,
            speech,
            new SpellingWorkflowNoValuePrompt(speech));
    }

    private static SpellingBlockReadback CreateBlockReadback(
        List<string> spoken,
        bool spellOutAllowed)
    {
        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy
            .Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(spellOutAllowed);

        var renderer = new Mock<IPhoneticSpellOutRenderer>(MockBehavior.Loose);
        renderer
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => $"PHON[{value}]");

        return new SpellingBlockReadback(
            CreateSpeech(spoken),
            privacy.Object,
            renderer.Object);
    }

    private static IFormVoiceCommandSpeechService CreateSpeech(List<string> spoken)
    {
        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                spoken.Add(text);
                return Task.CompletedTask;
            });
        return speech.Object;
    }

    private static SpellingWorkflowBuffer CreateBuffer(params string[] blocks)
    {
        var buffer = new SpellingWorkflowBuffer();
        foreach (var block in blocks)
        {
            buffer.Append(block, confidencePercent: null);
        }

        return buffer;
    }

    private static EvaluationSession CreateSession()
        => new("test-session", EvaluationChannel.Voice, "test-app", "form-1");
}
