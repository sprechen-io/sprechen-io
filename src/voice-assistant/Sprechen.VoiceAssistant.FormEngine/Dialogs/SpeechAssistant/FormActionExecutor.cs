using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Regelaktionen ihren spezialisierten Ausführern zu und sammelt Ansagekennungen
/// über den dafür vorgesehenen Ausgabepuffer.
/// </summary>
internal sealed class FormActionExecutor
{
    private readonly FormVisibilityActionExecutor _visibilityActions;
    private readonly FormFieldAccessActionExecutor _fieldAccessActions;
    private readonly FormVariableValueActionExecutor _variableValueActions;
    private readonly FormNavigationActionExecutor _navigationActions;
    private readonly IReadOnlyDictionary<FormActionKind, Func<JsonElement, List<string>, bool>> _dispatchers;

    public FormActionExecutor(
        IFormActionVisibilityState visibilityState,
        IFormActionFieldAccessState fieldAccessState,
        IFormActionVariableValueState variableValueState,
        IFormActionNavigationState navigationState)
    {
        _visibilityActions = new FormVisibilityActionExecutor(visibilityState);
        _fieldAccessActions = new FormFieldAccessActionExecutor(fieldAccessState);
        _variableValueActions = new FormVariableValueActionExecutor(variableValueState);
        _navigationActions = new FormNavigationActionExecutor(navigationState);
        _dispatchers = CreateDispatchers();
    }

    /// <summary>
    /// Führt eine erkannte unterstützte Einzelaktion über ihren zuständigen Ausführer aus.
    /// </summary>
    /// <param name="action">Das JSON-Element der Aktion.</param>
    /// <param name="promptIdsToSpeak">Der Sammelpuffer für Kennungen aus Ansageaktionen.</param>
    /// <returns>Ob der Ausführer eine für die Regeliteration relevante Zustandsänderung meldet.
    /// Das bloße Sammeln einer Ansagekennung meldet keine solche Änderung.</returns>
    public bool ApplyAction(JsonElement action, List<string> promptIdsToSpeak)
    {
        if (!FormActionKindResolver.TryResolve(action, out var kind))
        {
            return false;
        }

        if (!_dispatchers.TryGetValue(kind, out var apply))
        {
            return false;
        }

        return apply(action, promptIdsToSpeak);
    }

    private IReadOnlyDictionary<FormActionKind, Func<JsonElement, List<string>, bool>> CreateDispatchers()
        => new Dictionary<FormActionKind, Func<JsonElement, List<string>, bool>>
        {
            [FormActionKind.SetVisible] = (action, _) => _visibilityActions.ApplySetVisible(action),
            [FormActionKind.SetEnabled] = (action, _) => _fieldAccessActions.ApplySetEnabled(action),
            [FormActionKind.SetReadOnly] = (action, _) => _fieldAccessActions.ApplySetReadOnly(action),
            [FormActionKind.SetValue] = (action, _) => _variableValueActions.ApplySetValue(action),
            [FormActionKind.ClearValue] = (action, _) => _variableValueActions.ApplyClearValue(action),
            [FormActionKind.CopyValue] = (action, _) => _variableValueActions.ApplyCopyValue(action),
            [FormActionKind.Speak] = FormSpeakActionExecutor.ApplySpeak,
            [FormActionKind.NavigateToFieldGroup] = (action, _) => _navigationActions.ApplyNavigateToFieldGroup(action),
            [FormActionKind.FocusField] = (action, _) => _navigationActions.ApplyFocusField(action)
        };
}
