using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Behandelt Rücksprünge aus der Formularnavigation und liefert das passende Dialogergebnis oder fordert einen Hinweis an.
/// </summary>
internal sealed class FormVoiceCommandSwitchFormNavigationExit
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandSwitchFormNavigationExit(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task<FormVoiceCommandSwitchFormNavigationResult> ResolveAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.Equals(context.CommandId, GlobalCommandIds.BackToFormSelection, StringComparison.Ordinal))
        {
            return await ResolveBackToFormSelectionAsync(context, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(context.CommandId, GlobalCommandIds.BackToAppSelection, StringComparison.Ordinal)
            || string.Equals(context.CommandId, GlobalCommandIds.OpenApplications, StringComparison.Ordinal))
        {
            return FormVoiceCommandSwitchFormNavigationResult.Handled(
                VoiceCommandDialogResult.BackToAppSelection(
                    context.Contract.ApplicationId,
                    context.CurrentForm.FormId,
                    (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                        .ToImmutableDictionary(StringComparer.Ordinal)));
        }

        return FormVoiceCommandSwitchFormNavigationResult.NotHandled;
    }

    private async Task<FormVoiceCommandSwitchFormNavigationResult> ResolveBackToFormSelectionAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        if (context.Application.Forms.Count <= 1)
        {
            await SpeakSingleFormOnlyAsync(context, cancellationToken).ConfigureAwait(false);
            return FormVoiceCommandSwitchFormNavigationResult.Handled(null);
        }

        return FormVoiceCommandSwitchFormNavigationResult.Handled(
            VoiceCommandDialogResult.BackToFormSelection(
                context.Contract.ApplicationId,
                context.CurrentForm.FormId,
                (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                    .ToImmutableDictionary(StringComparer.Ordinal)));
    }

    private async Task SpeakSingleFormOnlyAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        _evaluationTelemetry.TrackNavigationError(
            context.Session,
            NavigationErrorCodes.SingleFormOnly,
            ConstraintScopes.CrossGroup);

        await _speechService.SpeakSafeAsync(
                FormNavigationSwitchPromptBuilder.GetSingleFormOnlyPrompt(
                    context.Contract,
                    context.IsoCode,
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
