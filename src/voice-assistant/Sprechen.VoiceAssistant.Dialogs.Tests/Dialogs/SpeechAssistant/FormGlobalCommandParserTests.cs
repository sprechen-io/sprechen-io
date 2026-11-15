using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormGlobalCommandParserTests
{
    [Fact]
    public void TryParse_WhenLocalizedUtteranceUsesInformalVariant_ResolvesByPoliteness()
    {
        var contract = CreateContract(
            [
                new GlobalCommandDefinition
                {
                    CommandId = CommandId.From(GlobalCommandIds.Status),
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de@formal"] = "status",
                            ["de@informal"] = "wo stehe ich"
                        }
                    ]
                }
            ]);

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "wo stehe ich",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Informal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.Status, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsInputModeCommand_ParsesSpellingMode()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "buchstabieren",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.InputModeSpelling, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsKeyboardModeCommand_ParsesKeyboardMode()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "tastatureingabe",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.InputModeKeyboard, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsDigitsModeCommand_ParsesDigitsMode()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "ziffernmodus",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.InputModeDigits, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsLiteralModeCommand_ParsesLiteralMode()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "w\u00f6rtlich",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.InputModeLiteral, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsLiteralOffCommand_ParsesLiteralOffMode()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "w\u00f6rtlich aus",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.InputModeLiteralOff, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsEditCommand_ParsesEditValueCommand()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "korrigieren",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.EditValue, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsFieldGroupListCommand_ParsesFieldGroupListCommand()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "welche feldgruppen",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.FieldGroupList, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsFieldListCommand_ParsesFieldListCommand()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "welche felder",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.FieldList, commandId);
        Assert.Null(argument);
    }

    [Theory]
    [InlineData("welche befehle")]
    [InlineData("was kann ich sagen")]
    public void TryParse_WhenFallbackContainsCommandListUtterance_ParsesCommandListCommand(string transcript)
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript,
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.CommandList, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFieldListUtteranceUsed_DoesNotShadowToCommandList()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "welche felder",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.FieldList, commandId);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsOpenApplicationsCommand_ParsesOpenApplications()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "anwendungskatalog \u00f6ffnen",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.OpenApplications, commandId);
        Assert.Null(argument);
    }

    [Theory]
    [InlineData("anwendungskatalog \u00f6ffnen")]
    [InlineData("anwendungen \u00f6ffnen")]
    public void TryParse_WhenOpenApplicationsUtteranceSpoken_ParsesOpenApplications(string transcript)
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript,
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.OpenApplications, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsChangeLanguageCommand_ParsesChangeLanguage()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "change language",
            isoCode: "en",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.ChangeLanguage, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsFrenchEditCommand_ParsesEditValueCommand()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "modifier",
            isoCode: "fr",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.EditValue, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenChangeLanguageCommandContainsTarget_ExtractsArgument()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "change language to english please",
            isoCode: "en",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.ChangeLanguage, commandId);
        Assert.Equal("to english", argument);
    }

    [Fact]
    public void TryParse_WhenFallbackContainsChangePolitenessCommand_ParsesChangePoliteness()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "anrede wechseln",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.ChangePoliteness, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenChangePolitenessCommandContainsTarget_ExtractsArgument()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "change politeness to informal please",
            isoCode: "en",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.ChangePoliteness, commandId);
        Assert.Equal("to informal", argument);
    }

    [Fact]
    public void TryParse_WhenCommandModelIdIsUnknown_FallsBackToDefaultCatalog()
    {
        var contract = CreateContract();
        contract.Projections!.Voice!.Hints!.CommandModelId = "voice.unknown.v1";

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "weiter",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.Next, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFormOverrideUsesExistingCommandId_ReplacesBaselineUtterances()
    {
        var contract = CreateContract(
            [
                new GlobalCommandDefinition
                {
                    CommandId = CommandId.From(GlobalCommandIds.Status),
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "lagebericht"
                        }
                    ]
                }
            ]);

        var overriddenParsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "lagebericht",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var overriddenCommandId,
            out var overriddenArgument);

        var baselineParsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "status",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var baselineCommandId,
            out var baselineArgument);

        Assert.True(overriddenParsed);
        Assert.Equal(GlobalCommandIds.Status, overriddenCommandId);
        Assert.Null(overriddenArgument);
        Assert.False(baselineParsed);
        Assert.Equal(string.Empty, baselineCommandId);
        Assert.Null(baselineArgument);
    }

    [Fact]
    public void TryParse_WhenFormOverrideDefinesSameCommandTwice_LastDefinitionWins()
    {
        var contract = CreateContract(
            [
                new GlobalCommandDefinition
                {
                    CommandId = CommandId.From(GlobalCommandIds.Status),
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "alter status"
                        }
                    ]
                },
                new GlobalCommandDefinition
                {
                    CommandId = CommandId.From(GlobalCommandIds.Status),
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "neuer status"
                        }
                    ]
                }
            ]);

        var lastParsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "neuer status",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var lastCommandId,
            out var lastArgument);

        var firstParsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "alter status",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var firstCommandId,
            out var firstArgument);

        Assert.True(lastParsed);
        Assert.Equal(GlobalCommandIds.Status, lastCommandId);
        Assert.Null(lastArgument);
        Assert.False(firstParsed);
        Assert.Equal(string.Empty, firstCommandId);
        Assert.Null(firstArgument);
    }

    [Fact]
    public void TryParse_WhenFormOverrideDisablesBaselineCommand_DisablesCommandForForm()
    {
        var contract = CreateContract(
            [
                new GlobalCommandDefinition
                {
                    CommandId = CommandId.From(GlobalCommandIds.Status),
                    Enabled = false,
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "status"
                        }
                    ]
                }
            ]);

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "status",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.False(parsed);
        Assert.Equal(string.Empty, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WhenFallbackCommandRequiresArgument_ExtractsArgument()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "wechsle zu teil 2",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.SwitchForm, commandId);
        Assert.Equal("teil 2", argument);
    }

    [Fact]
    public void TryParse_WhenArgumentContainsTrailingPoliteNoise_TrimsArgument()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "wechsle zu teil 2 bitte",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.SwitchForm, commandId);
        Assert.Equal("teil 2", argument);
    }

    [Fact]
    public void TryParse_WhenArgumentContainsOnlyPoliteNoise_ReturnsFalse()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "wechsle zu bitte",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.False(parsed);
        Assert.Equal(string.Empty, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_GeheZuFormularWithArgument_ResolvesSwitchForm()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "gehe zu formular kontakt",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.SwitchForm, commandId);
        Assert.Equal("kontakt", argument);
    }

    [Fact]
    public void TryParse_GeheZuWithArgument_ResolvesJumpTo()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "gehe zu nachname",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.JumpTo, commandId);
        Assert.Equal("nachname", argument);
    }

    [Fact]
    public void TryParse_GeheZurWithArgument_ResolvesJumpTo()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "gehe zur bonusfrage",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.JumpTo, commandId);
        Assert.Equal("bonusfrage", argument);
    }

    [Fact]
    public void TryParse_GeheZumWithArgument_ResolvesJumpTo()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "gehe zum hauptpreis",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.JumpTo, commandId);
        Assert.Equal("hauptpreis", argument);
    }

    [Fact]
    public void TryParse_GeheZuFormularExact_ResolvesSwitchForm()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "gehe zu formular",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.SwitchForm, commandId);
        Assert.Null(argument);
    }

    [Fact]
    public void TryParse_WechsleZuWithArgument_ResolvesSwitchForm()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "wechsle zu kontakt",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.SwitchForm, commandId);
        Assert.Equal("kontakt", argument);
    }

    [Fact]
    public void TryParse_SpringeZuWithArgument_ResolvesJumpTo()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "springe zu nachname",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out var argument);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.JumpTo, commandId);
        Assert.Equal("nachname", argument);
    }

    [Fact]
    public void TryStripCommandPrefix_WhenGermanPrefixPresent_StripsPrefix()
    {
        var stripped = FormGlobalCommandParser.TryStripCommandPrefix(
            transcript: "Befehl: weiter",
            isoCode: "de",
            out var strippedTranscript);

        Assert.True(stripped);
        Assert.Equal("weiter", strippedTranscript);
    }

    [Fact]
    public void TryStripCommandPrefix_WithContractAwareOverload_WhenGermanPrefixPresent_StripsPrefix()
    {
        var contract = CreateContract();

        var stripped = FormGlobalCommandParser.TryStripCommandPrefix(
            contract,
            transcript: "Befehl: weiter",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var strippedTranscript);

        Assert.True(stripped);
        Assert.Equal("weiter", strippedTranscript);
    }

    [Fact]
    public void TryStripCommandPrefix_WhenSpanishPrefixPresent_StripsPrefix()
    {
        var stripped = FormGlobalCommandParser.TryStripCommandPrefix(
            transcript: "comando siguiente",
            isoCode: "es",
            out var strippedTranscript);

        Assert.True(stripped);
        Assert.Equal("siguiente", strippedTranscript);
    }

    [Fact]
    public void ResolveCommandLabel_WhenSpokenFormIsMissing_IgnoresUtteranceArray()
    {
        var contract = CreateContract(
            [
                new GlobalCommandDefinition
                {
                    CommandId = CommandId.From(GlobalCommandIds.Next),
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "weiter",
                            ["fr"] = "continuer"
                        }
                    ]
                }
            ]);
        contract.SupportedLanguages = ["de", "en", "fr"];

        var label = FormGlobalCommandParser.ResolveCommandLabel(
            contract,
            GlobalCommandIds.Next,
            "fr",
            PolitenessLevel.Formal);

        Assert.Null(label);
    }

    [Fact]
    public void ResolveCommandLabel_WhenSpokenFormIsMissing_DoesNotFallBackToFirstUtterance()
    {
        var contract = CreateContract();

        var label = FormGlobalCommandParser.ResolveCommandLabel(
            contract,
            GlobalCommandIds.Next,
            "it",
            PolitenessLevel.Formal);

        Assert.Null(label);
    }

    [Fact]
    public void TryParse_WhenExactCommandMatch_ReturnsCommand()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "weiter",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.Next, commandId);
    }

    [Fact]
    public void TryParse_WhenCommandWordIsEmbeddedInOkayPrefix_ReturnsCommand()
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript: "okay weiter",
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.Next, commandId);
    }

    // Die Tests übergeben die zulässigen Befehlspräfixe ausdrücklich an den Parser.
    private static readonly string[] GuideFamilyForms = ["okay guide", "ok guide"];

    [Fact]
    public void HasCommandLeadIn_WhenTranscriptStartsWithAcceptedAnchorAndCommandBody_ReturnsTrue()
    {
        Assert.True(FormGlobalCommandParser.HasCommandLeadIn("okay guide weiter", GuideFamilyForms));
        // Auch die Erkennungsvariante „ok guide“ ist als Präfix zugelassen.
        Assert.True(FormGlobalCommandParser.HasCommandLeadIn("ok guide hilfe", GuideFamilyForms));
    }

    [Fact]
    public void HasCommandLeadIn_WhenTranscriptDoesNotStartWithAcceptedAnchor_ReturnsFalse()
    {
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("weiter bitte", GuideFamilyForms));
        // „okay“ allein gehört nicht zu den hier zugelassenen Präfixen.
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("okay weiter", GuideFamilyForms));
        // Ein Präfix als Teil eines längeren Wortes genügt nicht.
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("okayguidexyz weiter", GuideFamilyForms));
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("okguidexyz hilfe", GuideFamilyForms));
        // Zwischen Präfix und Befehl muss eine Wortgrenze liegen.
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("okguidehilfe", GuideFamilyForms));
    }

    [Theory]
    [InlineData("okguide hilfe", "ok guide")] // Zusammengezogene Erkennungsvariante mit getrenntem Befehl.
    [InlineData("okayguide weiter", "okay guide")] // Zusammengezogenes Aktivierungspräfix mit getrenntem Befehl.
    [InlineData("okayasistente ayuda", "okay asistente")] // Spanisches Präfix ohne inneres Leerzeichen, danach ein getrennter Befehl.
    public void HasCommandLeadIn_GluedRendering_AcceptsLeadIn(string transcript, string acceptedForm)
    {
        // Das fehlende Leerzeichen innerhalb des Präfixes verhindert die Erkennung nicht.
        Assert.True(FormGlobalCommandParser.HasCommandLeadIn(transcript, [acceptedForm]));
    }

    [Theory]
    [InlineData("okay asistente ayuda", "okay asistente")] // Spanisch.
    [InlineData("okay guida aiuto", "okay guida")] // Italienisch.
    public void HasCommandLeadIn_PerLanguage_AcceptsThatLanguagesForm(string transcript, string acceptedForm)
    {
        Assert.True(FormGlobalCommandParser.HasCommandLeadIn(transcript, [acceptedForm]));
    }

    [Fact]
    public void HasCommandLeadIn_DoesNotAcceptAnotherLanguagesForm()
    {
        // Mit dem italienischen Präfix ist „okay guide“ nicht zugelassen.
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("okay guide help", ["okay guida"]));
    }

    [Theory]
    [InlineData("okay guide")]
    [InlineData("Okay Guide")]
    [InlineData("Okay Guide.")]
    [InlineData("ok guide")]
    public void IsBareCommandAnchor_WhenTranscriptIsOnlyTheAnchor_ReturnsTrue(string transcript)
    {

        Assert.True(FormGlobalCommandParser.IsBareCommandAnchor(transcript, GuideFamilyForms));
    }

    [Theory]
    [InlineData("okay guide weiter")] // Präfix mit folgendem Befehl.
    [InlineData("okay guide hilfe")]
    [InlineData("weiter")]
    [InlineData("hilfeweg 12")]
    [InlineData("okayguidexyz")] // Das Präfix ist nur Teil eines längeren Wortes.
    [InlineData("okguidexyz")] // Auch zusammengezogen nur Teil eines längeren Wortes.
    [InlineData("okay")] // Kein vollständiges zugelassenes Präfix.
    public void IsBareCommandAnchor_WhenTranscriptHasMoreThanTheAnchor_ReturnsFalse(string transcript)
    {
        Assert.False(FormGlobalCommandParser.IsBareCommandAnchor(transcript, GuideFamilyForms));
    }

    [Theory]
    [InlineData("okguide", "ok guide")] // Zusammengezogene Erkennungsvariante ohne Befehl.
    [InlineData("okayguide", "okay guide")] // Zusammengezogenes Aktivierungspräfix ohne Befehl.
    [InlineData("okayasistente", "okay asistente")] // Spanisches Präfix ohne inneres Leerzeichen und ohne Befehl.
    public void IsBareCommandAnchor_GluedRendering_ReturnsTrue(string transcript, string acceptedForm)
    {
        // Auch ohne inneres Leerzeichen wird das alleinstehende Präfix erkannt.
        Assert.True(FormGlobalCommandParser.IsBareCommandAnchor(transcript, [acceptedForm]));
    }

    [Fact]
    public void Parser_FoldsDiacritics_WhenMatchingAccentedAcceptedForm()
    {
        // Das künstliche Beispiel mit Akzent prüft die Normalisierung innerhalb des Parsers:
        // Ein Transkript ohne Akzent muss zur zugelassenen Form mit Akzent passen.
        Assert.True(FormGlobalCommandParser.HasCommandLeadIn("okay guia ayuda", ["okay guía"]));
        Assert.True(FormGlobalCommandParser.IsBareCommandAnchor("okay guía", ["okay guía"]));
        Assert.True(FormGlobalCommandParser.IsBareCommandAnchor("okay guia", ["okay guía"]));
    }

    [Theory]
    [InlineData("ok")]
    [InlineData("okay")]
    [InlineData("Okay")]
    public void BareLeadInToken_WithNoName_IsNeitherLeadInNorBareAnchor(string transcript)
    {
        // „ok“ und „okay“ ohne Namen sind weder vollständige Befehlspräfixe noch
        // alleinstehende zusammengesetzte Präfixe. Ihre mögliche Bedeutung als Zustimmung
        // wird getrennt über die yesTokens des Sprachlexikons behandelt.
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn(transcript, GuideFamilyForms));
        Assert.False(FormGlobalCommandParser.IsBareCommandAnchor(transcript, GuideFamilyForms));
    }

    [Fact]
    public void IsIsolatedCommandSignal_WhenExactRepeatUtterance_ReturnsTrue()
    {
        var contract = CreateContract();

        var isIsolated = FormGlobalCommandParser.IsIsolatedCommandSignal(
            contract,
            transcript: "wiederholen",
            commandId: GlobalCommandIds.Repeat,
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal);

        Assert.True(isIsolated);
    }

    [Fact]
    public void IsIsolatedCommandSignal_WhenPolitePaddingAroundRepeatUtterance_ReturnsTrue()
    {
        var contract = CreateContract();

        var isIsolated = FormGlobalCommandParser.IsIsolatedCommandSignal(
            contract,
            transcript: "bitte wiederholen",
            commandId: GlobalCommandIds.Repeat,
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal);

        Assert.True(isIsolated);
    }

    [Fact]
    public void IsIsolatedCommandSignal_WhenRepeatIsEmbeddedInFlowingText_ReturnsFalse()
    {
        var contract = CreateContract();

        var isIsolated = FormGlobalCommandParser.IsIsolatedCommandSignal(
            contract,
            transcript: "meine adresse ist wiederholen strasse 1",
            commandId: GlobalCommandIds.Repeat,
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal);

        Assert.False(isIsolated);
    }

    [Theory]
    [InlineData("nachstes offenes pflichtfeld")]
    [InlineData("zum nachsten auszufullenden feld")]
    public void TryParse_NextOpenRequiredFieldUtterance_ResolvesToNextOpenRequiredField(string transcript)
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript,
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.NextOpenRequiredField, commandId);
    }

    [Theory]
    [InlineData("weiter")]
    [InlineData("nächstes feld")]
    public void TryParse_PlainNextUtterance_StillResolvesToNext_NotShadowedByOpenRequired(string transcript)
    {
        var contract = CreateContract();

        var parsed = FormGlobalCommandParser.TryParse(
            contract,
            transcript,
            isoCode: "de",
            politenessLevel: PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed);
        Assert.Equal(GlobalCommandIds.Next, commandId);
    }

    private static FormContract CreateContract(List<GlobalCommandDefinition>? globalCommands = null)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = false,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name", ["en"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Hauptteil", ["en"] = "Main" },
                    Members = [new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("name") }]
                }
            ],
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        GlobalCommands = globalCommands
                    }
                }
            }
        };
    }
}
