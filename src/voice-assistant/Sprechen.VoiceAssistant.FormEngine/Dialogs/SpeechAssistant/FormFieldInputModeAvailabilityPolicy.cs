using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft einen angeforderten Eingabemodus zuerst gegen die Typmatrix und anschließend
/// gegen Bearbeitbarkeit, Geheimhaltung und modellierte Spracheingaberegeln. Bevorzugte
/// Modi bestimmen die Auswahlreihenfolge, erteilen aber keine zusätzliche Freigabe.
/// </summary>
internal sealed class FormFieldInputModeAvailabilityPolicy
{
    private static readonly ImmutableHashSet<string> DigitFriendlyFormatHints = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "phone",
        "postalCode",
        "pin",
        "digits");

    private readonly FormMutableStateStore _mutableState;
    private readonly ImmutableDictionary<VariableId, EffectiveSensitiveDataPolicy> _effectivePoliciesByVariableId;
    private readonly Dictionary<FieldId, long> _deniedFieldAnnouncementRevisions = [];

    public FormFieldInputModeAvailabilityPolicy(
        FormMutableStateStore mutableState,
        ImmutableDictionary<VariableId, EffectiveSensitiveDataPolicy> effectivePoliciesByVariableId)
    {
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        _effectivePoliciesByVariableId = effectivePoliciesByVariableId
            ?? throw new ArgumentNullException(nameof(effectivePoliciesByVariableId));
    }

    /// <summary>
    /// Liefert eine gespeicherte Modusabweichung unverändert oder den ersten zugelassenen
    /// bevorzugten Modus. Der abschließende Ersatzwert ist Sprache. Seine Verfügbarkeit
    /// wird durch diese Rückgabe allein nicht zugesichert.
    /// </summary>
    public InputMode ResolveInputMode(FieldDefinition field, VariableDefinition variable, string fieldId)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        var fieldIdentity = FieldId.From(fieldId);
        if (_mutableState.FieldInputModeOverrides.TryGetValue(fieldIdentity, out var overrideInputMode)
            && InputModeExtensions.TryFromWireValue(overrideInputMode, out var resolvedOverride))
        {
            return resolvedOverride;
        }

        if (field.PreferredInputModes is not null)
        {
            foreach (var preferredMode in field.PreferredInputModes)
            {
                if (InputModeExtensions.TryFromWireValue(preferredMode, out var candidate)
                    && Evaluate(field, variable, candidate).Status == FormFieldInputModeAvailabilityStatus.Available)
                {
                    return candidate;
                }
            }
        }

        return InputMode.Voice;
    }

    public IReadOnlyList<string>? ResolvePreferredInputModes(FieldDefinition field, string fieldId)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        var fieldIdentity = FieldId.From(fieldId);
        if (_mutableState.FieldInputModeOverrides.TryGetValue(fieldIdentity, out var overrideInputMode))
        {
            return [overrideInputMode];
        }

        return field.PreferredInputModes;
    }

    public FormFieldInputModeAvailability Evaluate(
        FieldDefinition field,
        VariableDefinition variable,
        InputMode requestedMode)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(variable);

        var valueTypeKind = ReadValueTypeProperty(variable, ValueTypePropertyNames.Kind);
        var effectiveType = FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, valueTypeKind);
        var formatHint = ReadValueTypeProperty(variable, ValueTypePropertyNames.FormatHint);

        if (!IsSupportedByBaseMatrix(effectiveType, formatHint, requestedMode))
        {
            return FormFieldInputModeAvailability.Denied(FormFieldInputModeUnavailabilityReason.UnsupportedForFieldType);
        }

        if (field.Editability is FieldEditability.ReadOnly or FieldEditability.Computed
            || _mutableState.IsFieldReadOnly(field.FieldId))
        {
            return FormFieldInputModeAvailability.Denied(FormFieldInputModeUnavailabilityReason.FieldNotEditable);
        }

        var isMicrophoneMode = requestedMode is not InputMode.Keyboard;
        var effectivePolicy = RequirePolicy(variable.VariableId);

        // Den Geheimhaltungsvorbehalt für Mikrofoneingabe vor der modellierten
        // Spracheingaberegel anwenden, damit dessen Ablehnungsgrund Vorrang hat.
        if (effectivePolicy.PrivacyClass == PrivacyClasses.Secret && isMicrophoneMode)
        {
            return FormFieldInputModeAvailability.Denied(FormFieldInputModeUnavailabilityReason.BlockedByPrivacyPolicy);
        }

        if (isMicrophoneMode && !effectivePolicy.AllowsVoiceInput)
        {
            return FormFieldInputModeAvailability.Denied(FormFieldInputModeUnavailabilityReason.VoiceInputForbidden);
        }

        return FormFieldInputModeAvailability.Allowed;
    }

    /// <summary>
    /// Merkt die Behandlung einer Ablehnung für Feld und aktuelle Zustandsrevision.
    /// </summary>
    public void RecordDeniedFieldAnnouncement(FieldId fieldId)
        => _deniedFieldAnnouncementRevisions[fieldId] = _mutableState.StateRevision;

    /// <summary>
    /// Prüft den Ablehnungsmerker für genau dieses Feld und die aktuelle Revision,
    /// damit derselbe Zustand eine Rückmeldung nicht erneut anfordern muss.
    /// </summary>
    public bool HasDeniedFieldAnnouncement(FieldId fieldId)
        => _deniedFieldAnnouncementRevisions.TryGetValue(fieldId, out var revision)
            && revision == _mutableState.StateRevision;

    private static bool IsSupportedByBaseMatrix(string effectiveType, string? formatHint, InputMode mode)
        => mode switch
        {
            InputMode.Voice or InputMode.Keyboard => true,
            InputMode.Spelling or InputMode.Literal =>
                effectiveType is EffectiveFieldInteractionTypes.Text or EffectiveFieldInteractionTypes.Code,
            InputMode.Digits =>
                effectiveType is EffectiveFieldInteractionTypes.Code or EffectiveFieldInteractionTypes.Number
                || (effectiveType == EffectiveFieldInteractionTypes.Text
                    && formatHint is not null
                    && DigitFriendlyFormatHints.Contains(formatHint)),
            _ => false
        };

    private EffectiveSensitiveDataPolicy RequirePolicy(VariableId variableId)
    {
        if (!_effectivePoliciesByVariableId.TryGetValue(variableId, out var policy))
        {
            throw new InvalidOperationException(
                "A resolved privacy policy was required for the requested variable and was not present.");
        }

        return policy;
    }

    private static string? ReadValueTypeProperty(VariableDefinition variable, string propertyName)
        => variable.ValueType.ValueKind == JsonValueKind.Object
            && variable.ValueType.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
