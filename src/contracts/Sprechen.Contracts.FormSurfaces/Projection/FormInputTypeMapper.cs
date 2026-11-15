using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Verknüpft die grundlegende Typzuordnung mit feldspezifischen Anpassungen, damit jedes Feld einen abschließenden Eingabetyp erhält.
/// </summary>
internal sealed class FormInputTypeMapper
{
    private readonly FormBaseInputControlMapper _baseInputControlMapper;
    private readonly FormInputControlOverrideSelector _overrideSelector;
    private readonly FormValueTypeKindReader _valueTypeKindReader;

    public FormInputTypeMapper()
        : this(
            new FormBaseInputControlMapper(new FormFormatHintInputControlMapper()),
            new FormInputControlOverrideSelector(),
            new FormValueTypeKindReader())
    {
    }

    internal FormInputTypeMapper(
        FormBaseInputControlMapper baseInputControlMapper,
        FormInputControlOverrideSelector overrideSelector,
        FormValueTypeKindReader valueTypeKindReader)
    {
        _baseInputControlMapper = baseInputControlMapper ?? throw new ArgumentNullException(nameof(baseInputControlMapper));
        _overrideSelector = overrideSelector ?? throw new ArgumentNullException(nameof(overrideSelector));
        _valueTypeKindReader = valueTypeKindReader ?? throw new ArgumentNullException(nameof(valueTypeKindReader));
    }

    public string MapInputType(
        string valueTypeKind,
        VariableDefinition? variable,
        FieldDefinition field,
        FieldConstraints fieldConstraints,
        IReadOnlyList<FormSurfaceOption>? options)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(fieldConstraints);

        var inputType = _baseInputControlMapper.Map(valueTypeKind, variable);
        return _overrideSelector.Select(valueTypeKind, inputType, field, fieldConstraints, options);
    }

    public string? TryGetValueTypeKind(JsonElement valueType)
    {
        return _valueTypeKindReader.TryGetValueTypeKind(valueType);
    }
}
