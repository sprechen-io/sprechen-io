using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandTranscriptInterpretationServiceTests
{
    [Fact]
    public void Interpret_WhenCommandPrefixIsUsed_ParsesAndExecutesCommand()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "kommando weiter");

        Assert.True(interpretation.HasExplicitCommandPrefix);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Next, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
        Assert.False(interpretation.ShouldExecuteParsedCommandInDigitsMode);
        Assert.Equal(CommandSignalClass.Clear, interpretation.CommandSignalClass);
    }

    [Fact]
    public void Interpret_WhenCommandWordEmbeddedInFlowingText_StaysContent()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "ich gehe weiter nach hause");

        // Das eingebettete „weiter“ wird erkannt, aber ohne Aktivierungsform nicht zur Ausführung freigegeben.
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Next, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
        Assert.Equal(CommandSignalClass.NoCommand, interpretation.CommandSignalClass);
    }

    [Theory]
    [InlineData("weiter", GlobalCommandIds.Next)]
    [InlineData("hilfe", GlobalCommandIds.Help)]
    [InlineData("abbrechen", GlobalCommandIds.Cancel)]
    [InlineData("wiederholen", GlobalCommandIds.Repeat)]
    public void Interpret_WhenIsolatedCommandWordWithoutAnchor_StaysContent(string transcript, string expectedCommandId)
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: transcript);

        // Die vier gewöhnlichen Befehlswörter werden hier ohne Aktivierungsform nicht ausgeführt.
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(expectedCommandId, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
        Assert.Equal(CommandSignalClass.NoCommand, interpretation.CommandSignalClass);
    }

    [Theory]
    [InlineData("okay weiter", GlobalCommandIds.Next)]
    [InlineData("okay hilfe", GlobalCommandIds.Help)]
    [InlineData("okay abbrechen", GlobalCommandIds.Cancel)]
    [InlineData("okay wiederholen", GlobalCommandIds.Repeat)]
    public void Interpret_WhenCommandWordCarriesOkayAnchor_ExecutesCommand(string transcript, string expectedCommandId)
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: transcript);

        // Die für diesen Test konfigurierte Aktivierungsform „okay“ erlaubt die Befehlsausführung.
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(expectedCommandId, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
        Assert.Equal(CommandSignalClass.Clear, interpretation.CommandSignalClass);
    }

    [Fact]
    public void Interpret_WhenTextContainsRepeatInsideFlowingText_DoesNotAutoExecute()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "meine adresse ist wiederholen strasse 1");

        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Repeat, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
        Assert.Equal(CommandSignalClass.NoCommand, interpretation.CommandSignalClass);
    }

    [Theory]
    [InlineData("wiederholen", GlobalCommandIds.Repeat)]
    [InlineData("abbrechen", GlobalCommandIds.Cancel)]
    public void Interpret_WhenDigitsModeAndBareCommandWord_StaysContent(string transcript, string expectedCommandId)
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Digits, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Digits,
            transcript: transcript);

        // Die beiden Befehlswörter bleiben im Ziffernmodus ohne Aktivierungsform für die weitere Eingabeinterpretation erhalten.
        Assert.True(interpretation.IsDigitsMode);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(expectedCommandId, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommandInDigitsMode);
    }

    [Theory]
    [InlineData("okay wiederholen", GlobalCommandIds.Repeat)]
    [InlineData("okay abbrechen", GlobalCommandIds.Cancel)]
    public void Interpret_WhenDigitsModeAndOkayAnchoredCommand_Executes(string transcript, string expectedCommandId)
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Digits, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Digits,
            transcript: transcript);

        Assert.True(interpretation.IsDigitsMode);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(expectedCommandId, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommandInDigitsMode);
    }

    [Fact]
    public void Interpret_WhenLeadingWakeWordCommandIsPresent_ExecutesCommandInTextField()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract, ["Okay"]);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "Okay weiter");

        Assert.True(interpretation.HasExplicitCommandPrefix);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Next, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
        Assert.Equal(CommandSignalClass.Clear, interpretation.CommandSignalClass);
    }

    [Fact]
    public void Interpret_WhenLiteralModeAndBareCommandWord_StaysContent()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Literal, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Literal,
            transcript: "weiter");

        Assert.True(interpretation.IsLiteralMode);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Next, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
    }

    [Theory]
    [InlineData("okay weiter", GlobalCommandIds.Next)]
    [InlineData("okay abbrechen", GlobalCommandIds.Cancel)]
    public void Interpret_WhenLiteralModeAndOkayAnchoredCommand_Executes(string transcript, string expectedCommandId)
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Literal, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Literal,
            transcript: transcript);

        Assert.True(interpretation.IsLiteralMode);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(expectedCommandId, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
    }

    [Theory]
    [InlineData("literal aus")]
    [InlineData("okay literal aus")]
    public void Interpret_WhenLiteralModeAndModeExit_ExecutesWithoutAnchor(string transcript)
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Literal, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Literal,
            transcript: transcript);

        // Der Befehl zum Beenden des wörtlichen Modus bleibt auch ohne Aktivierungsform ausführbar.
        Assert.True(interpretation.IsLiteralMode);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.InputModeLiteralOff, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
    }

    [Theory]
    [InlineData("buchstabieren", GlobalCommandIds.InputModeSpelling)]
    [InlineData("ziffernmodus", GlobalCommandIds.InputModeDigits)]
    [InlineData("spracheingabe", GlobalCommandIds.InputModeVoice)]
    [InlineData("tastatureingabe", GlobalCommandIds.InputModeKeyboard)]
    [InlineData("wörtlich", GlobalCommandIds.InputModeLiteral)]
    public void Interpret_WhenBareModeSwitchWordInValueField_EntersModeWithoutAnchor(
        string transcript,
        string expectedCommandId)
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: transcript);

        // Alleinstehende Modusbefehle werden außerhalb des wörtlichen Modus auch ohne Aktivierungsform zur Ausführung freigegeben.
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(expectedCommandId, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
        Assert.Equal(CommandSignalClass.Clear, interpretation.CommandSignalClass);
    }

    [Fact]
    public void Interpret_WhenBareSpellingWordInChoiceField_EntersSpellingWithoutAnchor()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateSingleSelectFieldSnapshot(
        [
            new FormChoiceOption("pflege", "Hilfe zur Pflege"),
            new FormChoiceOption("haushalt", "Hilfe im Haushalt")
        ]);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "buchstabieren");

        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.InputModeSpelling, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenAnchoredSpellingWordInValueField_StillEntersSpelling()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "okay buchstabieren");

        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.InputModeSpelling, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenLiteralModeAndBareSpellingWord_StaysVerbatimContent()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Literal, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Literal,
            transcript: "buchstabieren");

        // Im wörtlichen Modus wird „buchstabieren“ ohne Aktivierungsform nicht als Moduswechsel freigegeben.
        Assert.True(interpretation.IsLiteralMode);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.InputModeSpelling, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenModeWordIsEmbeddedInFlowingValue_StaysContent()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(inputMode: InputMode.Voice, codeStyle: false);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "ich moechte das jetzt buchstabieren bitte");

        Assert.False(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenNonTextOptionLabelSpokenVerbatim_DoesNotExecuteCommand()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateSingleSelectFieldSnapshot(
        [
            new FormChoiceOption("pflege", "Hilfe zur Pflege"),
            new FormChoiceOption("haushalt", "Hilfe im Haushalt")
        ]);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "hilfe zur pflege");

        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Help, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenNonTextOptionCodeSpokenVerbatim_DoesNotExecuteCommand()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateSingleSelectFieldSnapshot(
        [
            new FormChoiceOption("weiter", "Etwas Anderes"),
            new FormChoiceOption("haushalt", "Hilfe im Haushalt")
        ]);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "weiter");

        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Next, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenNonTextIsolatedCommandWord_StaysContent()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateSingleSelectFieldSnapshot(
        [
            new FormChoiceOption("pflege", "Hilfe zur Pflege"),
            new FormChoiceOption("haushalt", "Hilfe im Haushalt")
        ]);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "hilfe");

        // Im Auswahlfeld bleibt „hilfe“ ohne Aktivierungsform Eingabe; eine Optionsauswahl wird hier noch nicht durchgeführt.
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Help, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenNonTextOkayAnchoredCommandWord_ExecutesCommand()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateSingleSelectFieldSnapshot(
        [
            new FormChoiceOption("pflege", "Hilfe zur Pflege"),
            new FormChoiceOption("haushalt", "Hilfe im Haushalt")
        ]);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "okay hilfe");

        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Help, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenNonTextExplicitCommandPrefixCollidesWithOption_ExecutesCommand()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateSingleSelectFieldSnapshot(
        [
            new FormChoiceOption("pflege", "Hilfe zur Pflege"),
            new FormChoiceOption("haushalt", "Hilfe im Haushalt")
        ]);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "befehl hilfe");

        Assert.True(interpretation.HasExplicitCommandPrefix);
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Help, interpretation.ParsedCommandId);
        Assert.True(interpretation.ShouldExecuteParsedCommand);
    }

    [Fact]
    public void Interpret_WhenNonTextSafetyCommandWordCollidesWithOptionLabel_StaysContentSelection()
    {
        var service = new FormVoiceCommandTranscriptInterpretationService();
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateSingleSelectFieldSnapshot(
        [
            new FormChoiceOption("abbrechen", "Abbrechen"),
            new FormChoiceOption("weiter", "Weiter")
        ]);

        var interpretation = service.Interpret(
            context,
            field,
            expectedInputMode: InputMode.Voice,
            transcript: "abbrechen");

        // Die Option „Abbrechen“ darf ohne Aktivierungsform keinen Abbruchbefehl auslösen.
        Assert.True(interpretation.HasParsedCommand);
        Assert.Equal(GlobalCommandIds.Cancel, interpretation.ParsedCommandId);
        Assert.False(interpretation.ShouldExecuteParsedCommand);
    }

    private static FormFieldSnapshot CreateSingleSelectFieldSnapshot(ImmutableArray<FormChoiceOption> options)
    {
        return new FormFieldSnapshot(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "choice",
                VariableId: "choice",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Auswahl",
                HelpText: null,
                Examples: [],
                Options: options),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.Enum,
                SpeakValueInConfirmation: true));
    }

    private static FormFieldSnapshot CreateFieldSnapshot(InputMode inputMode, bool codeStyle)
    {
        return new FormFieldSnapshot(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "name",
                VariableId: "name",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: codeStyle,
                InputMode: inputMode,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));
    }

    private static FormContract CreateMinimalContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };
    }

    private static FormVoiceCommandContext CreateContext(FormContract contract, ImmutableArray<string> wakeWordSuffixes = default)
    {
        // Diese Tests verwenden standardmäßig „okay“ als akzeptierte Aktivierungsform im Sitzungskontext.
        if (wakeWordSuffixes.IsDefaultOrEmpty)
        {
            wakeWordSuffixes = ["okay"];
        }

        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: wakeWordSuffixes),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
