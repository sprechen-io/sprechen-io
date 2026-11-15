using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFocusedFieldHelpSpeakerTests
{
    [Fact]
    public async Task SpeakAsync_WhenFocusedFieldHasHelpExamplesAndOptions_SpeaksFieldSegmentsThenCommandsPointer()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateField());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 3, total: 7));

        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);

        // Nach den Feldinformationen folgt ein Verweis auf die Befehlsliste; deren vollständiger Inhalt wird hier nicht eingefügt.
        Assert.Equal(6, speechService.Calls.Count);
        Assert.Equal("Feld 3 von 7 heißt Feld.", speechService.Calls[0].Text);
        Assert.Equal("Hilfetext", speechService.Calls[1].Text);
        Assert.Equal("Beispielwert", speechService.Calls[2].Text);
        Assert.Contains("1: Alpha", speechService.Calls[3].Text, StringComparison.Ordinal);
        Assert.Contains("2: Beta", speechService.Calls[3].Text, StringComparison.Ordinal);
        Assert.Contains("welche Befehle", speechService.Calls[4].Text, StringComparison.Ordinal);
        // Der letzte Text nennt ein Beispiel mit Aktivierungswort.
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[5].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpeakAsync_WhenFieldHasNoHelpExamplesOrOptions_SpeaksCommandsPointerThenAnchorHint()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateFieldWithoutHelp());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 1, total: 2));

        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);

        Assert.Equal(3, speechService.Calls.Count);
        Assert.Equal("Feld 1 von 2 heißt Feld.", speechService.Calls[0].Text);
        Assert.Contains("Befehle", speechService.Calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[2].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpeakAsync_OpensWithPositionAndLabel_BeforeExpectationExamplesAndOptions()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateField());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 3, total: 7));

        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);

        Assert.Equal(6, speechService.Calls.Count);
        Assert.Equal("Feld 3 von 7 heißt Feld.", speechService.Calls[0].Text);
        Assert.Equal("Hilfetext", speechService.Calls[1].Text);
        Assert.Equal("Beispielwert", speechService.Calls[2].Text);
        Assert.Contains("1: Alpha", speechService.Calls[3].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpeakAsync_WhenFieldHasNoHelpText_StillOpensWithPositionAndLabel()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateFieldWithoutHelp());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 1, total: 2));

        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);

        Assert.Equal(3, speechService.Calls.Count);
        Assert.Equal("Feld 1 von 2 heißt Feld.", speechService.Calls[0].Text);
        Assert.Contains("Befehle", speechService.Calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[2].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpeakAsync_WhenHelpTextIsWhitespaceOnly_SkipsTheHelpTextSegmentButStillOpens()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateFieldWithWhitespaceHelp());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 1, total: 2));

        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);

        Assert.Equal(3, speechService.Calls.Count);
        Assert.Equal("Feld 1 von 2 heißt Feld.", speechService.Calls[0].Text);
        Assert.Contains("welche Befehle", speechService.Calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[2].Text, StringComparison.Ordinal);
        Assert.All(speechService.Calls, call => Assert.False(string.IsNullOrWhiteSpace(call.Text)));
    }

    [Fact]
    public async Task SpeakAsync_WhenFieldHasNoHelpTextButOptions_SpeaksOpeningThenOptionsWithoutExpectationSegment()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateFieldWithOptionsButNoHelp());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 2, total: 5));

        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);

        Assert.Equal(4, speechService.Calls.Count);
        Assert.Equal("Feld 2 von 5 heißt Feld.", speechService.Calls[0].Text);
        Assert.Contains("1: Alpha", speechService.Calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("2: Beta", speechService.Calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("welche Befehle", speechService.Calls[2].Text, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[3].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpeakAsync_SecondHelpOnSameField_RepeatsFieldContentWithoutBoilerplate()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var stateTracker = new TestFormVoiceCommandDialogStateTracker();
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateField());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 3, total: 7));

        await speaker.SpeakAsync(runtime.Object, stateTracker, "de", new VoiceDialogSettings(), CancellationToken.None);
        await speaker.SpeakAsync(runtime.Object, stateTracker, "de", new VoiceDialogSettings(), CancellationToken.None);

        // Beim zweiten Hilfeaufruf für dasselbe Feld bleiben die Feldinformationen erhalten; die beiden Befehlshinweise entfallen.
        Assert.Equal(10, speechService.Calls.Count);
        var secondHelp = speechService.Calls.Skip(6).Select(call => call.Text).ToArray();
        Assert.Equal("Feld 3 von 7 heißt Feld.", secondHelp[0]);
        Assert.Equal("Hilfetext", secondHelp[1]);
        Assert.Equal("Beispielwert", secondHelp[2]);
        Assert.Contains("1: Alpha", secondHelp[3], StringComparison.Ordinal);
        Assert.All(secondHelp, text => Assert.DoesNotContain("welche Befehle", text, StringComparison.Ordinal));
        Assert.All(secondHelp, text => Assert.DoesNotContain("Okay Alex", text, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SpeakAsync_FirstHelpOnAnotherField_TeachesBoilerplateAgain()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var stateTracker = new TestFormVoiceCommandDialogStateTracker();
        var firstRuntime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        firstRuntime.SetupGet(item => item.Contract).Returns(CreateContract());
        firstRuntime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        firstRuntime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateFieldWithoutHelp(fieldId: "firstField"));
        firstRuntime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 1, total: 2));
        var secondRuntime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        secondRuntime.SetupGet(item => item.Contract).Returns(CreateContract());
        secondRuntime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        secondRuntime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateFieldWithoutHelp(fieldId: "secondField"));
        secondRuntime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 2, total: 2));

        await speaker.SpeakAsync(firstRuntime.Object, stateTracker, "de", new VoiceDialogSettings(), CancellationToken.None);
        await speaker.SpeakAsync(secondRuntime.Object, stateTracker, "de", new VoiceDialogSettings(), CancellationToken.None);

        // Der Zustandstracker merkt sich die Befehlshinweise je Feld; beim zweiten Feld werden sie erneut angefordert.
        Assert.Equal(6, speechService.Calls.Count);
        Assert.Contains("welche Befehle", speechService.Calls[1].Text, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[2].Text, StringComparison.Ordinal);
        Assert.Contains("welche Befehle", speechService.Calls[4].Text, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[5].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpeakAsync_WithFreshSessionTracker_TeachesBoilerplateAgain()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateFieldWithoutHelp());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 1, total: 2));

        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);
        await speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None);

        // Ein neuer Zustandstracker erlaubt die Befehlshinweise auch für dasselbe Feld erneut.
        Assert.Equal(6, speechService.Calls.Count);
        Assert.Contains("welche Befehle", speechService.Calls[4].Text, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", speechService.Calls[5].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SpeakAsync_LatchesFieldScopedTopicKeys_ExactlyOnceAcrossTwoHelps()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var stateTracker = new TestFormVoiceCommandDialogStateTracker();
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateField());
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 3, total: 7));

        await speaker.SpeakAsync(runtime.Object, stateTracker, "de", new VoiceDialogSettings(), CancellationToken.None);
        await speaker.SpeakAsync(runtime.Object, stateTracker, "de", new VoiceDialogSettings(), CancellationToken.None);

        // Die beiden Themenschlüssel enthalten die Feldkennung und werden jeweils einmal im Zustandstracker hinterlegt.
        Assert.Single(stateTracker.TaughtTeachingTopics, $"{DialogTeachOnceTopics.HelpCommandsPointer}:field");
        Assert.Single(stateTracker.TaughtTeachingTopics, $"{DialogTeachOnceTopics.HelpCommandAnchor}:field");
        Assert.Equal(2, stateTracker.TaughtTeachingTopics.Count);
    }

    [Fact]
    public async Task SpeakAsync_WhenFocusedFieldIdIsWhitespace_Throws()
    {
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandFocusedFieldHelpSpeaker(speechService);
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.SetupGet(item => item.Contract).Returns(CreateContract());
        runtime.SetupGet(item => item.PolitenessLevel).Returns(PolitenessLevel.Formal);
        runtime.Setup(item => item.GetFocusedFieldAsync()).ReturnsAsync(CreateField(fieldId: " "));
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(index: 3, total: 7));

        // Ohne gültige Feldkennung lassen sich die einmaligen Hinweise keinem Feld zuordnen; der Aufruf bricht daher vor der Ansage ab.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => speaker.SpeakAsync(runtime.Object, new TestFormVoiceCommandDialogStateTracker(), "de", new VoiceDialogSettings(), CancellationToken.None));
        Assert.Empty(speechService.Calls);
    }

    private static FormStatusSnapshot CreateStatusSnapshot(int index, int total)
    {
        return new FormStatusSnapshot(
            new FormStatusNavigationPosition(
                CurrentFormId: "current",
                CurrentFormIndex: 1,
                TotalForms: 1,
                CurrentFieldGroupId: "main",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 1,
                CurrentFieldId: "field"),
            new FormStatusCompletionProgress(
                new FormStatusProgressDimension(Completed: index - 1, Total: total),
                new FormStatusProgressDimension(Completed: 0, Total: total),
                new FormStatusProgressDimension(Completed: 0, Total: 1)),
            CurrentFieldLabel: "Feld",
            CurrentFieldIndex: index,
            CurrentFieldGroupTitle: "Main",
            OpenRequiredFieldCount: 0,
            ValidationErrorCount: 0);
    }

    private static FormFieldSnapshot CreateField(string fieldId = "field")
    {
        return new FormFieldSnapshot(
            new FormFieldIdentitySnapshot(fieldId, "variable", "main"),
            new FormFieldPresentationSnapshot(
                "Main",
                "Feld",
                "Hilfetext",
                ImmutableArray.Create("Beispielwert", " "),
                ImmutableArray.Create(
                    new FormChoiceOption("a", "Alpha"),
                    new FormChoiceOption("b", "Beta"))),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));
    }

    private static FormFieldSnapshot CreateFieldWithoutHelp(string fieldId = "field")
    {
        return new FormFieldSnapshot(
            new FormFieldIdentitySnapshot(fieldId, "variable", "main"),
            new FormFieldPresentationSnapshot(
                "Main",
                "Feld",
                HelpText: null,
                ImmutableArray<string>.Empty,
                ImmutableArray<FormChoiceOption>.Empty),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));
    }

    private static FormFieldSnapshot CreateFieldWithWhitespaceHelp(string fieldId = "field")
    {
        return new FormFieldSnapshot(
            new FormFieldIdentitySnapshot(fieldId, "variable", "main"),
            new FormFieldPresentationSnapshot(
                "Main",
                "Feld",
                HelpText: "   ",
                ImmutableArray<string>.Empty,
                ImmutableArray<FormChoiceOption>.Empty),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));
    }

    private static FormFieldSnapshot CreateFieldWithOptionsButNoHelp(string fieldId = "field")
    {
        return new FormFieldSnapshot(
            new FormFieldIdentitySnapshot(fieldId, "variable", "main"),
            new FormFieldPresentationSnapshot(
                "Main",
                "Feld",
                HelpText: null,
                ImmutableArray<string>.Empty,
                ImmutableArray.Create(
                    new FormChoiceOption("a", "Alpha"),
                    new FormChoiceOption("b", "Beta"))),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true
        };
    }
}
