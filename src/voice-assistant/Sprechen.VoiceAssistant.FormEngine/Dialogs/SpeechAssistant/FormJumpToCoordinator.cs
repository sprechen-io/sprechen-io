using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Sprungauflösung, Fokusversuch und Inhaltsprojektion, damit die Rückmeldung zwischen Eingabeziel und ausschließlich vorlesbarem Ziel unterscheiden kann.
/// </summary>
internal sealed class FormJumpToCoordinator
{
    private readonly FormRuntimeComposition _composition;
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormReadOnlyFieldReadbackProjector _readOnlyReadbackProjector;

    internal FormJumpToCoordinator(
        FormRuntimeComposition composition,
        IFormDialogRuntime runtime,
        IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(privacyReadbackPolicy);
        _composition = composition ?? throw new ArgumentNullException(nameof(composition));
        _definitionState = composition.DefinitionState;
        _readOnlyReadbackProjector = new FormReadOnlyFieldReadbackProjector(
            runtime,
            runtime,
            runtime,
            privacyReadbackPolicy,
            _definitionState.RequestedIsoCode);
    }

    internal async Task<FormJumpToResult> TryJumpToAsync(string target)
    {
        var execution = await _composition.NavigationExecutor.JumpToTargetAsync(FormJumpTarget.From(target)).ConfigureAwait(false);
        return execution.Kind switch
        {
            FormJumpExecutionKind.Focused => FormJumpToResult.Focused(),
            FormJumpExecutionKind.AudibleOnlyTarget => await ComposeSpokenReadOnlyTargetAsync(execution.Resolution!).ConfigureAwait(false),
            _ => FormJumpToResult.NotFound()
        };
    }

    // Für ein ausschließlich vorlesbares Sprungziel dessen Inhaltsprojektion bilden.
    // Felder erhalten ihre einzelne Vorleseprojektion, Gruppen ihren Titel und die
    // ausgewählten Inhalte. Dieser Schritt erstellt Daten. Die Wiedergabe folgt beim Aufrufer.
    private async Task<FormJumpToResult> ComposeSpokenReadOnlyTargetAsync(FormJumpTargetResolution resolution)
    {
        if (resolution.Kind == FormJumpTargetKind.Field)
        {
            return FormJumpToResult.ForReadOnlyField(
                await _readOnlyReadbackProjector.ProjectFieldReadbackAsync(resolution.TargetId).ConfigureAwait(false));
        }

        var fieldGroupTitle = _definitionState.LocalizationResolver.ResolveFieldGroupTitle(resolution.TargetId);
        var readbacks = await _readOnlyReadbackProjector.ProjectFieldGroupReadbacksAsync(resolution.TargetId).ConfigureAwait(false);
        return FormJumpToResult.ForReadOnlyFieldGroup(fieldGroupTitle, readbacks);
    }
}
