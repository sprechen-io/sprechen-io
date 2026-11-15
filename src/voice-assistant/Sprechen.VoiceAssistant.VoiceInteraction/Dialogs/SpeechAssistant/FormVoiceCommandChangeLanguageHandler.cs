using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft ein Sprachwechselziel gegen Anwendung und Formular und fordert dessen Bestätigung an, bevor ein Wechselergebnis mit den bisherigen Werten zurückgegeben wird.
/// </summary>
internal sealed class FormVoiceCommandChangeLanguageHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly LanguageChangeTargetResolver _languageResolver;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandChangeLanguageHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        LanguageChangeTargetResolver languageResolver,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _languageResolver = languageResolver ?? throw new ArgumentNullException(nameof(languageResolver));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => string.Equals(commandId, GlobalCommandIds.ChangeLanguage, StringComparison.Ordinal);

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var supportedLanguageIsoCodes = ResolveSupportedLanguageIsoCodes(context);
        var supportedLanguages = _languageResolver.FormatSupportedLanguages(
            supportedLanguageIsoCodes,
            context.IsoCode);

        if (string.IsNullOrWhiteSpace(context.Argument))
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.LanguageMissingArgument,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormDialogFlowPromptTextResolver.GetMissingArgumentPrompt(
                        context.IsoCode,
                        supportedLanguages,
                        context.VoiceSettings.GetPolitenessLevel()),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        if (!_languageResolver.TryResolve(
                context.Argument,
                context.IsoCode,
                out var targetIsoCode,
                out var targetDisplayName))
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.LanguageNotSupported,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormDialogFlowPromptTextResolver.GetUnsupportedPrompt(
                        context.IsoCode,
                        supportedLanguages),
                    context.IsoCode,
                    context.VoiceSettings,
                cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        if (!supportedLanguageIsoCodes.Contains(targetIsoCode, StringComparer.Ordinal))
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.LanguageNotSupported,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormDialogFlowPromptTextResolver.GetUnsupportedPrompt(
                        context.IsoCode,
                        supportedLanguages),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var currentIsoCode = IsoCode.TryNormalizeIso6391(context.IsoCode) ?? context.IsoCode.Trim().ToLowerInvariant();
        if (string.Equals(targetIsoCode, currentIsoCode, StringComparison.Ordinal))
        {
            _evaluationTelemetry.TrackNavigationError(
                context.Session,
                NavigationErrorCodes.LanguageAlreadySelected,
                ConstraintScopes.CrossGroup);

            await _speechService.SpeakSafeAsync(
                    FormDialogFlowPromptTextResolver.GetAlreadySelectedPrompt(
                        context.IsoCode,
                        targetDisplayName),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var confirm = await _speechService.ConfirmYesNoAsync(
                context.CaptureSettings,
                FormDialogFlowPromptTextResolver.GetConfirmationPrompt(
                    context.IsoCode,
                    targetDisplayName),
                context.IsoCode,
                new SpeechCaptureTelemetryContext(context.Session),
                cancellationToken)
            .ConfigureAwait(false);

        if (confirm is not true)
        {
            return null;
        }

        await _speechService.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetWakeWordHintPrompt(targetIsoCode, context.VoiceSettings.GetPolitenessLevel()),
                targetIsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return VoiceCommandDialogResult.ChangeLanguage(
            context.Contract.ApplicationId,
            context.CurrentForm.FormId,
            targetIsoCode,
            (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                .ToImmutableDictionary(StringComparer.Ordinal));
    }

    private static string[] ResolveSupportedLanguageIsoCodes(FormVoiceCommandGlobalCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Contract.SupportedLanguages
            .Intersect(context.Application.SupportedLanguages, StringComparer.OrdinalIgnoreCase)
            .Select(IsoCode.TryNormalizeIso6391)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
