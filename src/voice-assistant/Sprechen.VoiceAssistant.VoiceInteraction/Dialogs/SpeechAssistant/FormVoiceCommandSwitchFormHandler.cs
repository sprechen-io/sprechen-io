namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Zielauflösung, Voraussetzungen und Bestätigung für den globalen Formularwechselbefehl.
/// </summary>
internal sealed class FormVoiceCommandSwitchFormHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly FormVoiceCommandSwitchFormNavigationExit _navigationExit;
    private readonly FormVoiceCommandSwitchFormTargetResolver _targetResolver;
    private readonly FormVoiceCommandSwitchFormPrerequisiteGuard _prerequisiteGuard;
    private readonly FormVoiceCommandSwitchFormConfirmationFlow _confirmationFlow;

    public FormVoiceCommandSwitchFormHandler(
        FormVoiceCommandSwitchFormNavigationExit navigationExit,
        FormVoiceCommandSwitchFormTargetResolver targetResolver,
        FormVoiceCommandSwitchFormPrerequisiteGuard prerequisiteGuard,
        FormVoiceCommandSwitchFormConfirmationFlow confirmationFlow)
    {
        _navigationExit = navigationExit ?? throw new ArgumentNullException(nameof(navigationExit));
        _targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        _prerequisiteGuard = prerequisiteGuard ?? throw new ArgumentNullException(nameof(prerequisiteGuard));
        _confirmationFlow = confirmationFlow ?? throw new ArgumentNullException(nameof(confirmationFlow));
    }

    public bool CanHandle(CommandId commandId)
        => commandId.Value is GlobalCommandIds.SwitchForm
            or GlobalCommandIds.BackToFormSelection
            or GlobalCommandIds.BackToAppSelection
            or GlobalCommandIds.OpenApplications;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var navigationResult = await _navigationExit.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
        if (navigationResult.WasHandled)
        {
            return navigationResult.DialogResult;
        }

        var target = await _targetResolver.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return null;
        }

        var prerequisiteResult = await _prerequisiteGuard.EnsureSatisfiedAsync(context, target, cancellationToken).ConfigureAwait(false);
        if (prerequisiteResult is not null)
        {
            return prerequisiteResult;
        }

        return await _confirmationFlow.ConfirmAsync(context, target, cancellationToken).ConfigureAwait(false);
    }
}
