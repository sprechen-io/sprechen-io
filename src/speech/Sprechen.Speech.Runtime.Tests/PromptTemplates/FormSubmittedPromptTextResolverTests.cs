using Sprechen.Speech.ModelRuntime.PromptTemplates;

namespace Sprechen.Speech.Runtime.Tests.PromptTemplates;

public sealed class FormSubmittedPromptTextResolverTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void ResolvesFormSubmittedPrompt_PerLanguage(string isoCode)
    {
        var ack = FormDialogFlowPromptTextResolver.GetFormSubmittedPrompt(isoCode);

        Assert.False(string.IsNullOrWhiteSpace(ack));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void ResolvesPrivacySafeEvaluationDeliveryStates_PerLanguage(string isoCode)
    {
        var prompts = new[]
        {
            FormDialogFlowPromptTextResolver.GetEvaluationDeliveryWaitingPrompt(isoCode),
            FormDialogFlowPromptTextResolver.GetEvaluationOtherTabPrompt(isoCode),
            FormDialogFlowPromptTextResolver.GetEvaluationPermanentFailurePrompt(isoCode),
            FormDialogFlowPromptTextResolver.GetEvaluationLocalCommitWaitingPrompt(isoCode),
            FormDialogFlowPromptTextResolver.GetEvaluationTaskCancelledPrompt(isoCode),
            FormDialogFlowPromptTextResolver.GetEvaluationTechnicalFailurePrompt(isoCode),
            FormDialogFlowPromptTextResolver.GetEvaluationRestoredAbandonedPrompt(isoCode),
        };

        Assert.All(prompts, prompt =>
        {
            Assert.False(string.IsNullOrWhiteSpace(prompt));
            Assert.DoesNotContain("participant", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("event_id", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("task_id", prompt, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData(
        "de",
        "Die Aufgabe wurde abgebrochen.",
        "Die Eingabe wurde wegen eines technischen Fehlers beendet. Bitte starten Sie die Aufgabe erneut.",
        "Die vorherige Eingabe konnte nach dem Neuladen nicht fortgesetzt werden. Bitte starten Sie die Aufgabe erneut.")]
    [InlineData(
        "en",
        "The task was cancelled.",
        "The entry ended because of a technical problem. Please start the task again.",
        "The previous entry could not be continued after the page reloaded. Please start the task again.")]
    [InlineData(
        "fr",
        "La t\u00e2che a \u00e9t\u00e9 annul\u00e9e.",
        "La saisie a \u00e9t\u00e9 interrompue en raison d\u2019un probl\u00e8me technique. Veuillez recommencer la t\u00e2che.",
        "La saisie pr\u00e9c\u00e9dente n\u2019a pas pu \u00eatre poursuivie apr\u00e8s le rechargement de la page. Veuillez recommencer la t\u00e2che.")]
    [InlineData(
        "es",
        "La tarea fue cancelada.",
        "La entrada termin\u00f3 debido a un problema t\u00e9cnico. Vuelva a iniciar la tarea.",
        "No se pudo continuar la entrada anterior despu\u00e9s de recargar la p\u00e1gina. Vuelva a iniciar la tarea.")]
    [InlineData(
        "it",
        "L\u2019attivit\u00e0 \u00e8 stata annullata.",
        "L\u2019inserimento \u00e8 terminato a causa di un problema tecnico. Riavvii l\u2019attivit\u00e0.",
        "Non \u00e8 stato possibile continuare l\u2019inserimento precedente dopo il ricaricamento della pagina. Riavvii l\u2019attivit\u00e0.")]
    public void ResolvesTerminalParticipantOutcomesWithExactSemantics(
        string isoCode,
        string expectedCancellation,
        string expectedTechnicalFailure,
        string expectedRestoredAbandoned)
    {
        Assert.Equal(
            expectedCancellation,
            FormDialogFlowPromptTextResolver.GetEvaluationTaskCancelledPrompt(isoCode));
        Assert.Equal(
            expectedTechnicalFailure,
            FormDialogFlowPromptTextResolver.GetEvaluationTechnicalFailurePrompt(isoCode));
        Assert.Equal(
            expectedRestoredAbandoned,
            FormDialogFlowPromptTextResolver.GetEvaluationRestoredAbandonedPrompt(isoCode));
    }
}
