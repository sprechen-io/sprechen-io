using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandConfirmationTranscriptInterpreterTests
{
    [Fact]
    public void TryInterpret_WhenTranscriptIsAffirmative_ReturnsContinue()
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "ja");

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.Continuation, decision.Value.Kind);
    }

    [Fact]
    public void TryInterpret_WhenTranscriptIsNegative_ReturnsEdit()
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "nein");

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.CurrentValueEdit, decision.Value.Kind);
    }

    [Fact]
    public void TryInterpret_WhenTranscriptIsNegatedConfirmation_ReturnsEdit()
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "Nein, das stimmt nicht");

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.CurrentValueEdit, decision.Value.Kind);
    }

    [Fact]
    public void TryInterpret_WhenAcceptValueGoalWordSpoken_ReturnsContinue()
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "übernehmen");

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.Continuation, decision.Value.Kind);
    }

    // Ohne Aktivierungswort nehmen Abschlusswörter den Wert an; mit
    // Aktivierungswort bleibt Absenden ein globaler Formularbefehl.
    [Theory]
    [InlineData("fertig")]
    [InlineData("absenden")]
    public void TryInterpret_WhenDoneWordSpokenWithoutAnchor_ReturnsContinue(string transcript)
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), transcript);

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.Continuation, decision.Value.Kind);
    }

    [Fact]
    public void TryInterpret_WhenSubmitIsAnchored_ReturnsExecution()
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "Okay absenden");

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.Execution, decision.Value.Kind);
        Assert.Equal(GlobalCommandIds.Submit, decision.Value.CommandId);
    }

    [Fact]
    public void CommandDecisionMapper_NextIsNotConfirmationContinue()
    {
        var decision = new FormVoiceCommandConfirmationCommandDecisionMapper().Map(GlobalCommandIds.Next, null);

        Assert.Equal(ConfirmationDecisionKind.Execution, decision.Kind);
        Assert.Equal(GlobalCommandIds.Next, decision.CommandId);
    }

    [Theory]
    [InlineData(GlobalCommandIds.AcceptValue, ConfirmationDecisionKind.Continuation)]
    [InlineData(GlobalCommandIds.EditValue, ConfirmationDecisionKind.CurrentValueEdit)]
    [InlineData(GlobalCommandIds.InputModeSpelling, ConfirmationDecisionKind.SpellingSwitch)]
    [InlineData(GlobalCommandIds.Help, ConfirmationDecisionKind.Execution)]
    public void CommandDecisionMapper_MapsConfirmationCommands(
        string commandId,
        ConfirmationDecisionKind expectedKind)
    {
        var decision = new FormVoiceCommandConfirmationCommandDecisionMapper().Map(commandId, "argument");

        Assert.Equal(expectedKind, decision.Kind);
        if (expectedKind == ConfirmationDecisionKind.Execution)
        {
            Assert.Equal(commandId, decision.CommandId);
            Assert.Equal("argument", decision.CommandArgument);
        }
    }

    [Fact]
    public void MissingDecisionPolicy_AlwaysUndecided_NeverCommitsOnSilence()
    {
        var decision = new FormVoiceCommandConfirmationMissingDecisionPolicy().Resolve(hasTranscript: false);

        Assert.Equal(ConfirmationDecisionKind.Undecided, decision.Kind);
        Assert.Equal(ConfirmationUndecidedReason.Silence, decision.UndecidedReason);
    }

    [Fact]
    public void MissingDecisionPolicy_WithTranscript_IsUndecidedForUnrecognized()
    {
        var decision = new FormVoiceCommandConfirmationMissingDecisionPolicy().Resolve(hasTranscript: true);

        Assert.Equal(ConfirmationDecisionKind.Undecided, decision.Kind);
        Assert.Equal(ConfirmationUndecidedReason.Unrecognized, decision.UndecidedReason);
    }

    [Theory]
    [InlineData("Okay Hilfe")]
    [InlineData("Okay abbrechen")]
    public void TryInterpret_WhenGlobalCommandIsAnchored_ReturnsExecution(string transcript)
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), transcript);

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.Execution, decision.Value.Kind);
    }

    [Theory]
    [InlineData("hilfe")]
    [InlineData("abbrechen")]
    public void TryInterpret_WhenGlobalCommandLacksAnchor_DoesNotExecute(string transcript)
    {
        // Die gezeigten globalen Befehle ohne Aktivierungswort liefern keine
        // Entscheidung. Eine spätere Rückfrage oder Navigation wird hier nicht geprüft.
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), transcript);

        Assert.Null(decision);
    }

    [Theory]
    [InlineData("okay")]
    [InlineData("Okay")]
    public void TryInterpret_WhenBareOkay_StaysAffirmativeNotExecution(string transcript)
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), transcript);

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.Continuation, decision.Value.Kind);
    }

    [Theory]
    // Ein alleinstehendes ok oder okay darf keinen Befehl ausführen. Ob es als
    // Zustimmung gilt, hängt von den modellierten Ja-Wörtern der Sitzungssprache ab.
    [InlineData("de", "okay", true)]
    [InlineData("de", "ok", true)]
    [InlineData("en", "okay", true)]
    [InlineData("en", "ok", true)]
    [InlineData("it", "okay", true)]
    [InlineData("it", "ok", false)]
    [InlineData("fr", "okay", false)]
    [InlineData("fr", "ok", false)]
    [InlineData("es", "okay", false)]
    [InlineData("es", "ok", false)]
    public void TryInterpret_BareLeadInToken_FollowsYesTokensAsymmetry_NeverExecutes(
        string isoCode,
        string token,
        bool expectAffirmative)
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(isoCode), token);

        Assert.True(
            decision is null || decision.Value.Kind != ConfirmationDecisionKind.Execution,
            $"Bare '{token}' must never be a command anchor (Execution) in '{isoCode}'; got {decision?.Kind}.");

        if (expectAffirmative)
        {
            Assert.NotNull(decision);
            Assert.Equal(ConfirmationDecisionKind.Continuation, decision!.Value.Kind);
        }
        else
        {
            Assert.Null(decision);
        }
    }

    [Fact]
    public void TryInterpret_WhenCorrectionWordSpoken_ReturnsEditAnchorFree()
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "korrigieren");

        Assert.NotNull(decision);
        Assert.Equal(ConfirmationDecisionKind.CurrentValueEdit, decision.Value.Kind);
    }

    [Fact]
    public void TryInterpret_WhenNotUnderstood_ReturnsNull()
    {
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "blabla unsinn zzz");

        Assert.Null(decision);
    }

    [Fact]
    public void TryInterpret_WhenAnchorWithoutKnownCommand_DoesNotExecute()
    {
        // Ein Aktivierungswort ohne zuordenbaren Befehl darf keine Befehlsausführung
        // auslösen. Der Test lässt ausdrücklich sowohl null als auch eine andere Entscheidung zu.
        var interpreter = CreateInterpreter();

        var decision = interpreter.TryInterpret(CreateContext(), "Okay quatsch");

        Assert.True(
            decision is null || decision.Value.Kind != ConfirmationDecisionKind.Execution,
            $"An anchored phrase with no parseable command must not execute a command; got {decision?.Kind}.");
    }

    private static FormVoiceCommandConfirmationTranscriptInterpreter CreateInterpreter()
        => new(
            new FormVoiceCommandConfirmationCommandDecisionMapper(),
            new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()));

    private static FormVoiceCommandContext CreateContext() => CreateContext("de");

    private static FormVoiceCommandContext CreateContext(string isoCode)
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            SupportedLanguages = [isoCode],
            DefaultLanguage = isoCode,
            Forms = [new FormDescriptor { FormId = FormId.From("form") }]
        };
        var contract = new FormContract
        {
            ApplicationId = "app",
            FormId = FormId.From("form"),
            ContractVersion = "1",
            SupportedLanguages = [isoCode],
            DefaultLanguage = isoCode,
            Fields = [],
            FieldGroups = []
        };

        return new FormVoiceCommandContext(
            new FormVoiceCommandSessionIdentity(
                isoCode,
                "app",
                "form",
                // Die akzeptierten Aktivierungsformen werden über den Kontext vorgegeben.
                ["okay"]),
            new FormVoiceCommandSelectionContext(
                new VoiceDialogSettings(),
                application,
                ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                application.Forms[0],
                contract),
            new FormVoiceCommandValueSnapshot(
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                []));
    }
}
