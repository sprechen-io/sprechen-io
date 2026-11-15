using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Parsing beziehungsweise Nachnormalisierung mit Wertübernahme, Regeln und Validierung für fokussierte und ausdrücklich benannte Felder.
/// </summary>
internal sealed class FormTranscriptApplicator(
    FormDialogDefinitionState definitionState,
    FormMutableStateStore mutableState,
    FormDialogDependencies services)
{
    private readonly FormDialogDefinitionState _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
    private readonly FormMutableStateStore _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    private readonly FormDialogDependencies _services = services ?? throw new ArgumentNullException(nameof(services));

    public FormInputResult TryApplyTranscript(string transcript)
    {
        if (!TryResolveInputTarget(out var field, out var variableId))
        {
            return new FormInputResult(ValueCaptured: false, Violations: []);
        }

        if (string.IsNullOrWhiteSpace(transcript))
        {
            // Ein leeres Transkript nur für eine Menge ohne positive Mindestanzahl als leere
            // Menge übernehmen und anschließend Regeln und Validierung ausführen. Eine getrennte
            // Pflichtbedingung kann diesen Wert weiterhin zurückweisen. Bei anderen Typen bleibt
            // die leere Eingabe nicht parsbar.
            return AllowsExplicitEmptySetValue(variableId)
                ? CommitFocusedFieldValue(variableId, string.Empty)
                : new FormInputResult(ValueCaptured: false, Violations: []);
        }

        var outcome = ParseInput(field, variableId, transcript);
        if (!outcome.Success)
        {
            return new FormInputResult(ValueCaptured: false, Violations: [], ParseFailure: outcome.Failure);
        }

        return CommitFocusedFieldValue(variableId, outcome.CanonicalValue!);
    }

    /// <summary>
    /// Übernimmt einen bereits zusammengesetzten Wert über Nachnormalisierung und typisierte
    /// Formprüfung auf das fokussierte Feld. Die Buchstaben- oder Zifferngrammatik wird nicht
    /// erneut angewandt. Danach folgen Wertänderungsregeln und die gemeinsame Validierung.
    /// </summary>
    public FormInputResult TryApplyComposedValue(string composedValue)
    {
        if (!TryResolveInputTarget(out var field, out var variableId))
        {
            return new FormInputResult(
                ValueCaptured: false,
                Violations: [],
                NotCapturedReason: FormInputNotCapturedReason.NoInputTarget);
        }

        if (string.IsNullOrWhiteSpace(composedValue))
        {
            return new FormInputResult(
                ValueCaptured: false,
                Violations: [],
                NotCapturedReason: FormInputNotCapturedReason.BlankValue);
        }

        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_definitionState.VariablesById, variableId)
            ?? ValueTypeKinds.String;
        var canonicalValue = _services.ValueParser.NormalizeComposedSpellingValue(
            field.CodeStyle,
            valueTypeKind,
            composedValue);

        // Den zusammengesetzten Wert vor der Übernahme gegen den gebundenen Werttyp
        // prüfen. Ein Abschluss der Zusammensetzung allein garantiert diese Form nicht.
        if (canonicalValue.Length == 0)
        {
            return new FormInputResult(
                ValueCaptured: false,
                Violations: [],
                NotCapturedReason: FormInputNotCapturedReason.BlankValue);
        }

        return _services.ValueParser.IsComposedValueCompatibleWithType(valueTypeKind, canonicalValue)
            ? CommitFocusedFieldValue(variableId, canonicalValue)
            : new FormInputResult(
                ValueCaptured: false,
                Violations: [],
                NotCapturedReason: FormInputNotCapturedReason.ValueTypeMismatch);
    }

    private FormInputResult CommitFocusedFieldValue(string variableId, string canonicalValue)
    {
        _mutableState.SetVariableValue(variableId, canonicalValue);

        // Der Regelablauf übernimmt Wiederherstellung und Fehlerrückmeldung bei fehlender Konvergenz.
        _ = _services.RuleOrchestrationService.TryApplyRules(RuleScopes.OnValueChanged, out _);
        _services.Navigator.EnsureFocusIsValid();

        var focusedFieldId = _mutableState.FocusedFieldId;
        ArgumentNullException.ThrowIfNull(focusedFieldId);

        var violations = _services.ValidationService.ValidateField(focusedFieldId.Value);
        return new FormInputResult(ValueCaptured: true, violations);
    }

    // Dieser Vorentscheid erlaubt die leere Menge bei fehlender oder nicht positiver
    // Mindestanzahl. Weitere Bedingungen, insbesondere die Pflichtbedingung, werden
    // erst im anschließenden Validierungsweg geprüft.
    private bool AllowsExplicitEmptySetValue(string variableId)
    {
        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_definitionState.VariablesById, variableId);
        if (!string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.Ordinal))
        {
            return false;
        }

        var (minSelected, _) = FormVariableValueReader.GetSetSelectionBounds(_definitionState.VariablesById, variableId);
        return (minSelected ?? 0) <= 0;
    }

    /// <summary>
    /// Wendet ein Transkript über Parsing, Wertänderungsregeln und Validierung auf das
    /// ausdrücklich benannte Feld an. Der Zielpfad setzt den Fokus nicht selbst. Ausgeführte
    /// Regelaktionen können ihn dennoch verändern.
    /// </summary>
    public FormInputResult TryApplyTranscriptToField(FieldId fieldId, string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return new FormInputResult(ValueCaptured: false, Violations: []);
        }

        if (!_definitionState.FieldsById.TryGetValue(fieldId, out var field))
        {
            return new FormInputResult(ValueCaptured: false, Violations: []);
        }

        if (!_definitionState.FieldToVariableId.TryGetValue(fieldId, out var variableId))
        {
            return new FormInputResult(ValueCaptured: false, Violations: []);
        }

        var outcome = ParseInput(field, variableId, transcript);
        if (!outcome.Success)
        {
            return new FormInputResult(ValueCaptured: false, Violations: [], ParseFailure: outcome.Failure);
        }

        _mutableState.SetVariableValue(variableId, outcome.CanonicalValue!);

        // Der Regelablauf übernimmt Wiederherstellung und Fehlerrückmeldung bei fehlender Konvergenz.
        _ = _services.RuleOrchestrationService.TryApplyRules(RuleScopes.OnValueChanged, out _);

        var violations = _services.ValidationService.ValidateField(fieldId);
        return new FormInputResult(ValueCaptured: true, violations);
    }

    private bool TryResolveInputTarget(
        out FieldDefinition field,
        out string variableId)
    {
        field = default!;
        variableId = string.Empty;

        if (_mutableState.FocusedFieldId is not { } focusedFieldId)
        {
            return false;
        }

        if (!_definitionState.FieldsById.TryGetValue(focusedFieldId, out var resolvedField))
        {
            return false;
        }

        field = resolvedField;
        variableId = _definitionState.FieldToVariableId[focusedFieldId];
        return true;
    }

    private FormTranscriptParseOutcome ParseInput(
        FieldDefinition field,
        string variableId,
        string transcript)
    {
        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_definitionState.VariablesById, variableId)
            ?? ValueTypeKinds.String;
        var options = _definitionState.LocalizationResolver.ResolveChoiceOptions(variableId);
        var preferredInputModes = _services.InputModeAvailabilityPolicy.ResolvePreferredInputModes(field, field.FieldId);

        return _services.ValueParser.ParseInput(
            field.CodeStyle,
            valueTypeKind,
            transcript,
            options,
            preferredInputModes);
    }
}
