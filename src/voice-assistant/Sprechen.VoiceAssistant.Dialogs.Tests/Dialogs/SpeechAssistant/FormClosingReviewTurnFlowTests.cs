using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormClosingReviewTurnFlowTests
{
    [Fact]
    public async Task ExplicitSubmit_Submits_WithoutPlayingSuccessBeforeTerminalAcceptance()
    {
        var harness = new FormClosingReviewTurnFlowTestHarness("absenden");
        harness.RouteSubmitAsSubmitted();

        var result = await harness.RunAsync();

        Assert.NotNull(result.Terminal);
        Assert.Equal(EvaluationOutcome.Submitted, result.Terminal!.DialogResult?.Outcome);
        harness.VerifySubmitRouted(Times.Once());
        harness.Earcon.Verify(e => e.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(1, harness.CaptureCount);
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal),
            Assert.Single(harness.CapturePrompts));
    }

    [Theory]
    [InlineData("de", "absenden", "Kontaktkanäle ist E-Mail, SMS. Das wurde übernommen. Alle Felder sind ausgefüllt. Zum Überprüfen sagen Sie zusammenfassung, zum Ändern eines Feldes sagen Sie gehe zu und den Feldnamen, zum Absenden sagen Sie absenden.")]
    [InlineData("en", "submit", "Kontaktkanäle: E-Mail, SMS. Accepted. All fields are complete. Say summary to review your answers, say go to and the field name to change one, or say submit to send the form.")]
    [InlineData("fr", "envoyer", "Kontaktkanäle: E-Mail, SMS. Validé. Tous les champs sont remplis. Dites résumé pour vérifier vos réponses, dites aller à et le nom du champ pour en modifier un, ou dites envoyer pour soumettre le formulaire.")]
    [InlineData("es", "enviar", "Kontaktkanäle: E-Mail, SMS. Aceptado. Todos los campos están completos. Diga resumen para revisar sus respuestas, diga ir a y el nombre del campo para cambiar uno, o diga enviar para enviar el formulario.")]
    [InlineData("it", "invia", "Kontaktkanäle: E-Mail, SMS. Confermato. Tutti i campi sono compilati. Dica riepilogo per controllare le risposte, dica vai a e il nome del campo per modificarne uno, oppure dica invia per inviare il modulo.")]
    public async Task CommittedValueReadbackPrompt_FirstOfferPrefixesValue_AndKeepsActionCueLast(
        string isoCode,
        string submitTranscript,
        string expectedPrompt)
    {
        var harness = FormClosingReviewTurnFlowTestHarness.ForLanguage(isoCode, submitTranscript);
        harness.RouteSubmitAsSubmitted();
        var readbackPrompt = FormConfirmationPromptBuilder.GetCommitAcceptedNamingValuePrompt(
            isoCode,
            "Kontaktkanäle",
            "E-Mail, SMS");
        var closingPrompt = FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt(isoCode, PolitenessLevel.Formal);

        var result = await harness.RunAsync(readbackPrompt);

        Assert.Equal(EvaluationOutcome.Submitted, result.Terminal?.DialogResult?.Outcome);
        var prompt = Assert.Single(harness.CapturePrompts);
        Assert.Equal(expectedPrompt, prompt);
        Assert.StartsWith(string.Concat(readbackPrompt, " "), prompt, StringComparison.Ordinal);
        Assert.EndsWith(closingPrompt, prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommittedValueReadbackPrompt_ReOfferUsesBareClosingPrompt()
    {
        var harness = new FormClosingReviewTurnFlowTestHarness("<silence>", "absenden");
        harness.RouteSubmitAsSubmitted();
        var readbackPrompt = FormConfirmationPromptBuilder.GetCommitAcceptedNamingValuePrompt(
            "de",
            "Kontaktkanäle",
            "E-Mail, SMS");
        var closingPrompt = FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt("de", PolitenessLevel.Formal);

        var result = await harness.RunAsync(readbackPrompt);

        Assert.Equal(EvaluationOutcome.Submitted, result.Terminal?.DialogResult?.Outcome);
        Assert.Equal(
            [
                string.Concat(readbackPrompt, " ", closingPrompt),
                closingPrompt
            ],
            harness.CapturePrompts);
    }

    [Fact]
    public async Task Silence_NeverSubmits_AndSubmitsOnlyOnTheExplicitSubmitAfterBoundedReprompts()
    {
        // Zwei simulierte Meldungen ohne Sprache dürfen nicht absenden.
        // Erst der nachfolgende ausdrückliche Absendeauftrag wird zur Ausführung weitergegeben.
        var harness = new FormClosingReviewTurnFlowTestHarness("<silence>", "<silence>", "absenden");
        harness.RouteSubmitAsSubmitted();

        var result = await harness.RunAsync();

        Assert.Equal(EvaluationOutcome.Submitted, result.Terminal?.DialogResult?.Outcome);
        harness.VerifySubmitRouted(Times.Once());
        // Drei Erfassungsaufrufe: zweimal ohne Sprache, danach der Absendeauftrag.
        Assert.Equal(3, harness.CaptureCount);
        harness.VerifyNoCommandRoutedOtherThan("submit");
    }

    [Fact]
    public async Task UnrecognizedUtterance_NeverSubmits_AndReOffers()
    {
        var harness = new FormClosingReviewTurnFlowTestHarness("dieses hier ist kein befehl", "absenden");
        harness.RouteSubmitAsSubmitted();

        var result = await harness.RunAsync();

        Assert.Equal(EvaluationOutcome.Submitted, result.Terminal?.DialogResult?.Outcome);
        harness.VerifySubmitRouted(Times.Once());
        Assert.Equal(2, harness.CaptureCount);

        harness.VerifyNoCommandRoutedOtherThan("submit");
    }

    [Fact]
    public async Task NavigationCommand_AtClosingTurn_NeverSubmits_AndReOffers()
    {
        // „Weiter“ am Formularende darf nicht als Absendeauftrag wirken.
        // Der Abschlussdialog fragt erneut nach, bis der ausdrückliche Absendeauftrag folgt.
        var harness = new FormClosingReviewTurnFlowTestHarness("weiter", "absenden");
        harness.RouteSubmitAsSubmitted();

        var result = await harness.RunAsync();

        Assert.Equal(EvaluationOutcome.Submitted, result.Terminal?.DialogResult?.Outcome);
        harness.VerifySubmitRouted(Times.Once());
        // Zwei Erfassungsaufrufe: zuerst der Navigationsbefehl, danach der Absendeauftrag.
        Assert.Equal(2, harness.CaptureCount);
        // Der Navigationsbefehl wird hier nicht zur Ausführung weitergegeben.
        harness.VerifyNoCommandRoutedOtherThan("submit");
        // Die Erkennung des Navigationsbefehls bleibt in der Telemetrie von fehlender Sprache unterscheidbar.
        harness.VerifyCommandRecognized("next", Times.Once());
    }

    [Fact]
    public async Task BackCommand_AtClosingTurn_RoutesBack_AndReturnsToFieldLoop()
    {
        var harness = new FormClosingReviewTurnFlowTestHarness("zurück");
        harness.RouteBackAsContinue();
        harness.FocusedLastFieldThenPrevious();

        var result = await harness.RunAsync();

        Assert.Null(result.Terminal);
        harness.VerifyCommandRouted("back", Times.Once());
        harness.VerifySubmitRouted(Times.Never());
        Assert.Equal(DialogMacroStateIds.FieldGroupIntro, harness.StateTracker.CurrentMacroState);
    }

    [Fact]
    public async Task ReviewRequest_RoutesSummary_ThenReOffers_ThenSubmits()
    {
        var harness = new FormClosingReviewTurnFlowTestHarness("zusammenfassung", "absenden");
        harness.RouteSummaryAsContinue();
        harness.RouteSubmitAsSubmitted();
        harness.NoFocusedField();

        var result = await harness.RunAsync();

        Assert.Equal(EvaluationOutcome.Submitted, result.Terminal?.DialogResult?.Outcome);
        harness.Router.Verify(
            r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == "summary"), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
        harness.VerifySubmitRouted(Times.Once());
        Assert.Equal(2, harness.CaptureCount);
    }

    [Fact]
    public async Task ChangeRequest_JumpFocusesField_ReturnsToFieldLoop_AndLeavesProcessSummary()
    {
        var harness = new FormClosingReviewTurnFlowTestHarness("gehe zu farbe");
        harness.RouteJumpAsContinue();
        harness.FocusedFieldAfterJump();

        var result = await harness.RunAsync();

        // Ein Feldsprung führt ohne abschließendes Dialogergebnis zurück zur Feldbearbeitung.
        Assert.Null(result.Terminal);
        harness.Router.Verify(
            r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == "jumpTo"), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
        harness.Earcon.Verify(e => e.PlayAsync(EarconId.FlowDone, It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal(DialogMacroStateIds.FieldGroupIntro, harness.StateTracker.CurrentMacroState);
    }

    [Fact]
    public void ClosingTurn_TraversesProcessSummary_AsARealTurn()
    {
        // Der Abschlussdialog verwendet ProcessSummary. Die protokollierte Abgabe führt zu Submitted.
        var submitting = new FormVoiceCommandFieldGroupPromptState();
        submitting.BeginFieldPrompt("main");
        submitting.BeginClosingReview();
        Assert.Equal(DialogMacroStateIds.ProcessSummary, submitting.CurrentMacroState);
        submitting.RecordSubmitted();
        Assert.Equal(DialogMacroStateIds.Submitted, submitting.CurrentMacroState);

        // Eine Änderungsanforderung führt zurück zum Einstieg der Feldgruppe.
        var changing = new FormVoiceCommandFieldGroupPromptState();
        changing.BeginFieldPrompt("main");
        changing.BeginClosingReview();
        changing.RecordClosingReviewChangeRequested();
        Assert.Equal(DialogMacroStateIds.FieldGroupIntro, changing.CurrentMacroState);
    }

    [Fact]
    public void MidFormSubmit_StillFastForwardsThroughProcessSummary_AsTheSharedPath()
    {
        // Auch eine Abgabe aus der laufenden Feldbearbeitung muss Submitted erreichen.
        // Der Test prüft den Endzustand, nicht die einzelnen Zwischenübergänge.
        var state = new FormVoiceCommandFieldGroupPromptState();
        state.BeginFieldPrompt("main");
        Assert.Equal(DialogMacroStateIds.FieldLoop, state.CurrentMacroState);

        state.RecordSubmitted();

        Assert.Equal(DialogMacroStateIds.Submitted, state.CurrentMacroState);
    }

}
