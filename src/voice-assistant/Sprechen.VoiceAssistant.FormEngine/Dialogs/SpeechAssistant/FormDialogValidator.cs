using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Feld- und Formularbedingungen und baut geordnete Verstöße auf. Die Gesamtabfrage berücksichtigt sichtbare Felder und vermeidet wiederholte formularweite Meldungen.
/// </summary>
internal sealed class FormDialogValidator
{
    private readonly FormDialogValidationContext _context;
    private readonly FormRuleEngine _ruleEngine;
    private readonly FormConstraintValidator _constraintValidator;
    private readonly FormLocalizationResolver _localizationResolver;

    private static readonly FormOpenRulePrecedence OpenRulePrecedence = new();

    public FormDialogValidator(
        FormDialogValidationContext context,
        FormRuleEngine ruleEngine,
        FormConstraintValidator constraintValidator,
        FormLocalizationResolver localizationResolver)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
        _localizationResolver = localizationResolver ?? throw new ArgumentNullException(nameof(localizationResolver));
    }

    public ImmutableArray<FormConstraintViolation> ValidateAll(Func<string, bool> isFieldEffectivelyVisible)
    {
        ArgumentNullException.ThrowIfNull(isFieldEffectivelyVisible);

        var violations = new List<FormConstraintViolation>();
        var emittedWholeFormConstraintIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var fieldId in _context.OrderedFieldIds)
        {
            if (!isFieldEffectivelyVisible(fieldId))
            {
                continue;
            }

            foreach (var violation in ValidateField(fieldId))
            {
                // Eine formularweite Bedingung kann an mehreren sichtbaren Feldern geprüft werden.
                // Den ersten ausgegebenen Verstoß je Bedingungskennung als Anker behalten,
                // damit die Gesamtabfrage denselben Verstoß nicht je Feld wiederholt.
                if (IsWholeFormConstraint(violation.ConstraintId)
                    && !emittedWholeFormConstraintIds.Add(violation.ConstraintId))
                {
                    continue;
                }

                violations.Add(violation);
            }
        }

        return violations.ToImmutableArray();
    }

    private bool IsWholeFormConstraint(string constraintId)
    {
        foreach (var constraint in _context.Constraints)
        {
            if (string.Equals(constraint.ConstraintId, constraintId, StringComparison.Ordinal))
            {
                return string.Equals(constraint.Target.Kind, TargetKinds.Form, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    public ImmutableArray<FormConstraintViolation> ValidateField(string fieldId)
    {
        if (!_context.FieldsById.TryGetValue(FieldId.From(fieldId), out _))
        {
            return [];
        }

        var variableId = _context.FieldToVariableId[FieldId.From(fieldId)];
        var violations = new List<FormConstraintViolation>();

        foreach (var constraint in _context.Constraints)
        {
            if (!FormConstraintValidator.IsConstraintRelevantToField(constraint, fieldId, variableId))
            {
                continue;
            }

            if (constraint.When is { } whenExpression && whenExpression.ValueKind == JsonValueKind.Object)
            {
                if (!_ruleEngine.EvaluateExpression(whenExpression))
                {
                    continue;
                }
            }

            if (!FormVariableValueReader.TryGetValue(_context.VariablesById, _context.ValuesByVariableId, variableId, out var value))
            {
                value = default;
            }

            if (!IsConstraintSatisfied(constraint, variableId, value, _context.ValuesByVariableId.ContainsKey(variableId)))
            {
                violations.Add(BuildViolation(fieldId, constraint));
            }
        }

        return OrderViolationsForField(violations);
    }

    public ImmutableArray<FormConstraintViolation> OrderViolationsForField(IEnumerable<FormConstraintViolation> violations)
        => OpenRulePrecedence.Order(violations);

    private FormConstraintViolation BuildViolation(string fieldId, ConstraintDefinition constraint)
    {
        // Eine nicht aufgelöste Meldung hier leer lassen, statt die technische Kennung
        // als Ansagetext einzusetzen. Kennung und Code bleiben am Prüfergebnis erhalten.
        var message = _localizationResolver.ResolveText(constraint.Message, fallback: null);
        var remediation = constraint.Remediation is null ? null : _localizationResolver.ResolveText(constraint.Remediation, fallback: null);

        return new FormConstraintViolation(
            fieldId,
            constraint.ConstraintId,
            constraint.Severity == ConstraintSeverity.Unknown ? SeverityLevels.Error : constraint.Severity.ToWireToken(),
            constraint.Code,
            message,
            remediation,
            FormConstraintScopeResolver.Resolve(constraint),
            _context.FieldToFieldGroupId.TryGetValue(FieldId.From(fieldId), out var fieldGroupId) ? fieldGroupId : null);
    }

    private bool IsConstraintSatisfied(ConstraintDefinition constraint, string variableId, FormValue value, bool isValueSet)
    {
        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_context.VariablesById, variableId) ?? ValueTypeKinds.String;
        return _constraintValidator.IsConstraintSatisfied(constraint, variableId, value, isValueSet, valueTypeKind);
    }
}
