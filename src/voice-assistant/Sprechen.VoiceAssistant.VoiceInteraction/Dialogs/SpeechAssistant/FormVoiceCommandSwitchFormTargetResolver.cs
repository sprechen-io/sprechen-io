using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst ein Wechselargument gegen den Formularbestand auf. Bei höchstens einem Formular, fehlendem Argument, nicht gefundenem oder bereits gewähltem Ziel wird stattdessen die vorgesehene Ansage angefordert.
/// </summary>
internal sealed class FormVoiceCommandSwitchFormTargetResolver
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandFormMatcher _formMatcher;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandSwitchFormTargetResolver(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandFormMatcher formMatcher,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _formMatcher = formMatcher ?? throw new ArgumentNullException(nameof(formMatcher));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task<FormVoiceCommandSwitchFormTarget?> ResolveAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Application.Forms.Count <= 1)
        {
            await SpeakSingleFormOnlyAsync(context, cancellationToken).ConfigureAwait(false);
            return null;
        }

        if (string.IsNullOrWhiteSpace(context.Argument))
        {
            await _speechService.SpeakSafeAsync(
                    FormStatusPromptBuilder.BuildFormListPrompt(
                        context.Application,
                        context.Contract,
                        context.IsoCode,
                        context.VoiceSettings),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var orderedForms = new FormsCatalogFormOrder().OrderForms(context.Application).ToImmutableArray();

        if (!_formMatcher.TryResolveFormByArgument(
                context.Application,
                orderedForms,
                context.IsoCode,
                context.VoiceSettings,
                context.Argument,
                out var targetForm,
                out var targetName))
        {
            await SpeakFormNotFoundAsync(context, cancellationToken).ConfigureAwait(false);
            return null;
        }

        if (string.Equals(targetForm.FormId, context.CurrentForm.FormId, StringComparison.OrdinalIgnoreCase))
        {
            await SpeakAlreadySelectedAsync(context, targetName, cancellationToken).ConfigureAwait(false);
            return null;
        }

        return new FormVoiceCommandSwitchFormTarget(targetForm, targetName);
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

    private async Task SpeakFormNotFoundAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        _evaluationTelemetry.TrackNavigationError(
            context.Session,
            NavigationErrorCodes.FormNotFound,
            ConstraintScopes.CrossGroup);

        await _speechService.SpeakSafeAsync(
                FormNavigationSwitchPromptBuilder.GetFormNotFoundPrompt(
                    context.Contract,
                    context.IsoCode,
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SpeakAlreadySelectedAsync(
        FormVoiceCommandGlobalCommandContext context,
        string targetName,
        CancellationToken cancellationToken)
    {
        _evaluationTelemetry.TrackNavigationError(
            context.Session,
            NavigationErrorCodes.FormAlreadySelected,
            ConstraintScopes.CrossGroup);

        await _speechService.SpeakSafeAsync(
                FormNavigationSwitchPromptBuilder.GetAlreadyOnFormPrompt(
                    context.Contract,
                    context.IsoCode,
                    targetName,
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
