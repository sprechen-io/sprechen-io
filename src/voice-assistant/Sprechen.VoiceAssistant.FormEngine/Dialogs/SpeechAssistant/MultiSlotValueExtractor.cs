using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt zusätzliche Feldwertvorschläge aus Segmenten der Form Feldname: Wert.
/// Der normalisierte Name muss über die gemeinsame Zielauflösung ein anderes Feld ergeben,
/// und dessen Werttyp muss den Wert lesen können. Allein ein feldähnliches Wort genügt nicht.
/// </summary>
internal sealed class MultiSlotValueExtractor : IMultiSlotValueExtractor
{
    // Trennt den Feldnamen vom vorgeschlagenen Wert, etwa Stadt: Berlin.
    private const char NameValueDelimiter = ':';

    private static readonly char[] SegmentSeparators = [';', ',', '\n', '\r'];

    private readonly FormJumpTargetResolver _jumpTargetResolver;
    private readonly IFormValueParser _valueParser;
    private readonly FormFieldInputModeAvailabilityPolicy _inputModeAvailabilityPolicy;
    private readonly ImmutableDictionary<FieldId, FieldDefinition> _fieldsById;
    private readonly ImmutableDictionary<FieldId, string> _fieldToVariableId;
    private readonly ImmutableDictionary<VariableId, VariableDefinition> _variablesById;
    private readonly FormLocalizationResolver _localizationResolver;

    public MultiSlotValueExtractor(
        FormJumpTargetResolver jumpTargetResolver,
        IFormValueParser valueParser,
        FormFieldInputModeAvailabilityPolicy inputModeAvailabilityPolicy,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
        ImmutableDictionary<FieldId, string> fieldToVariableId,
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        FormLocalizationResolver localizationResolver)
    {
        _jumpTargetResolver = jumpTargetResolver ?? throw new ArgumentNullException(nameof(jumpTargetResolver));
        _valueParser = valueParser ?? throw new ArgumentNullException(nameof(valueParser));
        _inputModeAvailabilityPolicy = inputModeAvailabilityPolicy ?? throw new ArgumentNullException(nameof(inputModeAvailabilityPolicy));
        _fieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
        _fieldToVariableId = fieldToVariableId ?? throw new ArgumentNullException(nameof(fieldToVariableId));
        _variablesById = variablesById ?? throw new ArgumentNullException(nameof(variablesById));
        _localizationResolver = localizationResolver ?? throw new ArgumentNullException(nameof(localizationResolver));
    }

    public ImmutableArray<FormAdditionalFieldValueCandidate> Extract(string transcript, FieldId? focusedFieldId)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<FormAdditionalFieldValueCandidate>();
        var acceptedFieldIds = new HashSet<FieldId>();
        var ambiguousFieldIds = new HashSet<FieldId>();

        foreach (var segment in transcript.Split(SegmentSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryResolveCandidate(segment, focusedFieldId, out var candidate))
            {
                continue;
            }

            // Bei mehrfacher Nennung desselben Felds den Vorschlag für dieses Feld entfernen.
            // Das gilt auch bei gleichen Werten.
            if (!acceptedFieldIds.Add(candidate.FieldId))
            {
                ambiguousFieldIds.Add(candidate.FieldId);
                continue;
            }

            builder.Add(candidate);
        }

        if (ambiguousFieldIds.Count == 0)
        {
            return builder.ToImmutable();
        }

        return builder
            .Where(c => !ambiguousFieldIds.Contains(c.FieldId))
            .ToImmutableArray();
    }

    private bool TryResolveCandidate(
        string segment,
        FieldId? focusedFieldId,
        out FormAdditionalFieldValueCandidate candidate)
    {
        candidate = null!;

        var delimiterIndex = segment.IndexOf(NameValueDelimiter, StringComparison.Ordinal);
        if (delimiterIndex <= 0)
        {
            return false;
        }

        var nameSegment = segment[..delimiterIndex].Trim();
        var valueSegment = segment[(delimiterIndex + 1)..].Trim();
        if (nameSegment.Length == 0 || valueSegment.Length == 0)
        {
            return false;
        }

        if (!_jumpTargetResolver.TryResolve(FormJumpTarget.From(nameSegment), out var resolution)
            || resolution is null
            || resolution.Kind != FormJumpTargetKind.Field)
        {
            return false;
        }

        var fieldId = FieldId.From(resolution.TargetId);
        if (fieldId == focusedFieldId)
        {
            return false;
        }

        if (!_fieldsById.TryGetValue(fieldId, out var field))
        {
            return false;
        }

        if (!TryParseValueForField(field, fieldId, valueSegment, out var canonicalValue, out var valueTypeKind, out var options))
        {
            return false;
        }

        var label = _localizationResolver.ResolveText(field.Label, fallback: fieldId);
        candidate = new FormAdditionalFieldValueCandidate(
            fieldId,
            label,
            valueSegment,
            canonicalValue,
            valueTypeKind,
            options);
        return true;
    }

    private bool TryParseValueForField(
        FieldDefinition field,
        FieldId fieldId,
        string valueSegment,
        out string canonicalValue,
        out string valueTypeKind,
        out ImmutableArray<FormChoiceOption> options)
    {
        canonicalValue = string.Empty;
        valueTypeKind = ValueTypeKinds.String;
        options = [];
        if (!_fieldToVariableId.TryGetValue(fieldId, out var variableId))
        {
            return false;
        }

        valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_variablesById, variableId)
            ?? ValueTypeKinds.String;
        options = _localizationResolver.ResolveChoiceOptions(variableId);
        var preferredInputModes = _inputModeAvailabilityPolicy.ResolvePreferredInputModes(field, field.FieldId);

        return _valueParser.TryParseInput(
            field.CodeStyle,
            valueTypeKind,
            valueSegment,
            options,
            preferredInputModes,
            out canonicalValue);
    }
}
