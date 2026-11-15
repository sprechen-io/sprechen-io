using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die aufgelösten Formularvoraussetzungen und fordert bei fehlenden Abschlüssen einen Hinweis sowie die Rückkehr zur Auswahl an.
/// </summary>
internal sealed class FormVoiceCommandSwitchFormPrerequisiteGuard
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly FormVoiceCommandSwitchFormPrerequisiteResolver _prerequisiteResolver;

    public FormVoiceCommandSwitchFormPrerequisiteGuard(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandSwitchFormPrerequisiteResolver prerequisiteResolver)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _prerequisiteResolver = prerequisiteResolver ?? throw new ArgumentNullException(nameof(prerequisiteResolver));
    }

    public async Task<VoiceCommandDialogResult?> EnsureSatisfiedAsync(
        FormVoiceCommandGlobalCommandContext context,
        FormVoiceCommandSwitchFormTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(target);

        var missingPrerequisites = _prerequisiteResolver.ResolveMissingPrerequisiteNames(context, target);
        if (missingPrerequisites.Length == 0)
        {
            return null;
        }

        _evaluationTelemetry.TrackNavigationError(
            context.Session,
            NavigationErrorCodes.FormPrerequisitesMissing,
            ConstraintScopes.CrossGroup);

        await _speechService.SpeakSafeAsync(
                FormNavigationSwitchPromptBuilder.GetPrerequisitesNotMetPrompt(
                    context.Contract,
                    context.IsoCode,
                    target.DisplayName,
                    missingPrerequisites,
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return VoiceCommandDialogResult.BackToFormSelection(
            context.Contract.ApplicationId,
            context.CurrentForm.FormId,
            (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                .ToImmutableDictionary(StringComparer.Ordinal));
    }
}
