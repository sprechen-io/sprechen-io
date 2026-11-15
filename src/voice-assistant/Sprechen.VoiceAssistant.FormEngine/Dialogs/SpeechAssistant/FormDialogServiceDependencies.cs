using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Gemeinsame Instanzen von Zustand, Navigation und Interaktion des Formularlaufs.
/// </summary>
internal sealed record FormDialogServiceDependencies
{
    public FormDialogServiceDependencies(
        IFormTranscriptParser transcriptParser,
        IFormActionVisibilityState visibilityState,
        IFormActionFieldAccessState fieldAccessState,
        IFormActionVariableValueState variableValueState,
        IFormActionNavigationState actionNavigationState,
        IFormNavigatorState navigatorState)
    {
        ArgumentNullException.ThrowIfNull(transcriptParser);
        ArgumentNullException.ThrowIfNull(visibilityState);
        ArgumentNullException.ThrowIfNull(fieldAccessState);
        ArgumentNullException.ThrowIfNull(variableValueState);
        ArgumentNullException.ThrowIfNull(actionNavigationState);
        ArgumentNullException.ThrowIfNull(navigatorState);

        TranscriptParser = transcriptParser;
        VisibilityState = visibilityState;
        FieldAccessState = fieldAccessState;
        VariableValueState = variableValueState;
        ActionNavigationState = actionNavigationState;
        NavigatorState = navigatorState;
    }

    public IFormTranscriptParser TranscriptParser { get; }

    public IFormActionVisibilityState VisibilityState { get; }

    public IFormActionFieldAccessState FieldAccessState { get; }

    public IFormActionVariableValueState VariableValueState { get; }

    public IFormActionNavigationState ActionNavigationState { get; }

    public IFormNavigatorState NavigatorState { get; }
}
