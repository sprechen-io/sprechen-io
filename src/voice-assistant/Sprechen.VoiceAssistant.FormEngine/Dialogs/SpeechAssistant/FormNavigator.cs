using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Fokusprüfung, Nachbarfeldsuche und Feldgruppennavigation für das Formular,
/// damit Befehle diese Navigationswege gemeinsam nutzen.
/// </summary>
internal sealed class FormNavigator
{
    private readonly IFormNavigatorState _state;
    private readonly FormFieldFocusTargetResolver fieldFocusTargetResolver;
    private readonly FormFieldGroupFocusResolver fieldGroupFocusResolver;
    private readonly FormFocusRepairPolicy focusRepairService;

    public FormNavigator(IFormNavigatorState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        fieldFocusTargetResolver = new FormFieldFocusTargetResolver(_state);
        fieldGroupFocusResolver = new FormFieldGroupFocusResolver(_state);
        focusRepairService = new FormFocusRepairPolicy(_state);
    }

    /// <summary>
    /// Versucht, den Fokus auf das nächste verfügbare Nachbarfeld zu setzen.
    /// </summary>
    /// <returns>Ob ein entsprechendes Navigationsziel übernommen wurde.</returns>
    public bool TryMoveFocusNext()
    {
        return FocusNavigationTransitions.TryMoveNext(
            _state.CurrentFocusedFieldId,
            _state.OrderedFieldIds,
            _state.IsFieldInputAvailable,
            _state.TryGetFieldIndex,
            _state.SetCurrentFocus,
            out _,
            out _);
    }

    /// <summary>
    /// Versucht, den Fokus auf das vorherige verfügbare Nachbarfeld zu setzen.
    /// </summary>
    /// <returns>Ob ein entsprechendes Navigationsziel übernommen wurde.</returns>
    public bool TryMoveFocusBack()
    {
        return FocusNavigationTransitions.TryMoveBack(
            _state.CurrentFocusedFieldId,
            _state.OrderedFieldIds,
            _state.IsFieldInputAvailable,
            _state.TryGetFieldIndex,
            _state.SetCurrentFocus,
            out _,
            out _);
    }

    /// <summary>
    /// Versucht, den Fokus auf ein bekanntes und eingabeverfügbares Feld zu setzen.
    /// </summary>
    /// <returns>Ob das Ziel zugelassen und gesetzt wurde. Es kann bereits fokussiert gewesen sein.</returns>
    public bool TrySetFocus(string fieldId)
        => fieldFocusTargetResolver.TrySetFocus(fieldId);

    /// <summary>
    /// Versucht, das erste eingabeverfügbare Feld im Pfad der angegebenen Gruppe zu fokussieren.
    /// </summary>
    /// <returns>Ob ein gültiges Ziel übernommen wurde.</returns>
    public bool TryNavigateToFieldGroup(string fieldGroupId)
        => fieldGroupFocusResolver.TryNavigateToFieldGroup(fieldGroupId);

    /// <summary>
    /// Behält einen verfügbaren Fokus oder sucht ein Ersatzfeld. Fehlt jedes verfügbare
    /// Ziel, kann der bisherige ungültige Fokus unverändert bleiben.
    /// </summary>
    public void EnsureFocusIsValid()
        => focusRepairService.EnsureFocusIsValid();
}
