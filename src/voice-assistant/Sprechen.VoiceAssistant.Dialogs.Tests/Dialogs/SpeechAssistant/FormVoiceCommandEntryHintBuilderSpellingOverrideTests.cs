using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass der Feldeinstieg den Buchstabiermodus nur anbietet, wenn er
/// noch nicht aktiv ist. Die Auswertung einer späteren Antwort wird hier nicht geprüft.
/// </summary>
public sealed class FormVoiceCommandEntryHintBuilderSpellingOverrideTests
{
    [Fact]
    public async Task BuildAsync_TextFieldInVoiceMode_OffersSpelling()
    {
        var hint = await FormVoiceCommandEntryHintBuilder.BuildAsync(CreateTurnContext(InputMode.Voice));

        Assert.NotNull(hint);
        Assert.Contains("Hilfetext", hint, StringComparison.Ordinal);
        Assert.Contains("buchstabier", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildAsync_TextFieldAlreadyInSpellingMode_OffersNoSpelling()
    {
        var hint = await FormVoiceCommandEntryHintBuilder.BuildAsync(CreateTurnContext(InputMode.Spelling));

        Assert.Equal("Hilfetext", hint);
    }

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(InputMode inputMode)
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>())).ReturnsAsync(true);
        var field = new FormFieldSnapshot(
            Identity: new FormFieldIdentitySnapshot(FieldId: "name", VariableId: "name", FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
                HelpText: "Hilfetext",
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: inputMode,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

        return new FormVoiceCommandFieldTurnContext(
            SpellingWorkflowTestFactory.CreateContext("de"),
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            field,
            session,
            new FormVoiceCommandStateTracker(telemetry, session),
            new FormVoiceCommandAutoHelpTracker(
                new VoiceDialogSettings(),
                new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
                new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
                new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
                "de"),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object);
    }
}
