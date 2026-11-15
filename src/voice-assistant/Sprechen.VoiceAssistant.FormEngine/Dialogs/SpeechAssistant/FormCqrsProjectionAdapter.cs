namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Projektionen für Bearbeitung, Navigation und Status, damit Befehle nur den jeweils benötigten Zugriff erhalten.
/// </summary>
internal sealed class FormCqrsProjectionAdapter
{
    internal FormCqrsProjectionAdapter(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState,
        FormFieldVisibilityPolicy visibilityService,
        FormStatusProjectionBuilder statusProjectionService,
        FormDialogValidator validationService,
        Action onValueChanged)
    {
        var normalizedDefinitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        var normalizedMutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        var normalizedVisibilityService = visibilityService ?? throw new ArgumentNullException(nameof(visibilityService));

        Editing = new FormCqrsEditingProjection(
            normalizedDefinitionState,
            normalizedMutableState,
            onValueChanged ?? throw new ArgumentNullException(nameof(onValueChanged)));
        Navigation = new FormCqrsNavigationProjection(
            normalizedDefinitionState,
            normalizedMutableState,
            normalizedVisibilityService);
        FieldGroupNavigation = new FormCqrsFieldGroupNavigationProjection(
            normalizedDefinitionState,
            normalizedVisibilityService);
        Status = new FormCqrsStatusProjection(
            normalizedDefinitionState,
            normalizedMutableState,
            normalizedVisibilityService,
            statusProjectionService ?? throw new ArgumentNullException(nameof(statusProjectionService)),
            validationService ?? throw new ArgumentNullException(nameof(validationService)));
        Summary = new FormCqrsSummaryProjection(
            normalizedDefinitionState,
            normalizedMutableState);
    }

    internal FormCqrsEditingProjection Editing { get; }

    internal FormCqrsNavigationProjection Navigation { get; }

    internal FormCqrsFieldGroupNavigationProjection FieldGroupNavigation { get; }

    internal FormCqrsStatusProjection Status { get; }

    internal FormCqrsSummaryProjection Summary { get; }
}

