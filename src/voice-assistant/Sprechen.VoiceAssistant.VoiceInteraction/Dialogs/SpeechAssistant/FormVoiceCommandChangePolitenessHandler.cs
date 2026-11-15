using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die gewünschte Anrede anhand des aktuellen Sprachprofils und fordert
/// eine Bestätigung an, bevor das Ergebnis zum Wechseln zurückgegeben wird.
/// </summary>
internal sealed class FormVoiceCommandChangePolitenessHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly FormVoiceCommandPolitenessResolver _politenessResolver;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandChangePolitenessHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        FormVoiceCommandPolitenessResolver politenessResolver,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _politenessResolver = politenessResolver ?? throw new ArgumentNullException(nameof(politenessResolver));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => string.Equals(commandId, GlobalCommandIds.ChangePoliteness, StringComparison.Ordinal);

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var currentPolitenessLevel = context.VoiceSettings.GetPolitenessLevel();

        if (!_politenessResolver.HasPolitenessProfile(context.IsoCode))
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.PolitenessUnavailable,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormNavigationPolitenessPromptBuilder.GetUnavailablePrompt(
                        context.Contract,
                        context.IsoCode,
                        currentPolitenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var supportedOptions = _politenessResolver.FormatSupportedOptions(context.IsoCode);

        if (string.IsNullOrWhiteSpace(context.Argument))
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.PolitenessMissingArgument,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormNavigationPolitenessPromptBuilder.GetMissingArgumentPrompt(
                        context.Contract,
                        context.IsoCode,
                        supportedOptions,
                        currentPolitenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        if (!_politenessResolver.TryResolve(
                context.Argument,
                context.IsoCode,
                out var targetPolitenessLevel,
                out var targetLabel))
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.PolitenessNotSupported,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormNavigationPolitenessPromptBuilder.GetUnsupportedPrompt(
                        context.Contract,
                        context.IsoCode,
                        supportedOptions,
                        currentPolitenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        if (targetPolitenessLevel == currentPolitenessLevel)
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.PolitenessAlreadySelected,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormNavigationPolitenessPromptBuilder.GetAlreadySelectedPrompt(
                        context.Contract,
                        context.IsoCode,
                        targetLabel,
                        currentPolitenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var confirm = await _speechService.ConfirmYesNoAsync(
                context.CaptureSettings,
                FormNavigationPolitenessPromptBuilder.GetConfirmationPrompt(
                    context.Contract,
                    context.IsoCode,
                    targetLabel,
                    currentPolitenessLevel),
                context.IsoCode,
                new SpeechCaptureTelemetryContext(context.Session),
                cancellationToken)
            .ConfigureAwait(false);

        if (confirm is not true)
        {
            return null;
        }

        return VoiceCommandDialogResult.ChangePoliteness(
            context.Contract.ApplicationId,
            context.CurrentForm.FormId,
            targetPolitenessLevel,
            (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                .ToImmutableDictionary(StringComparer.Ordinal));
    }
}
