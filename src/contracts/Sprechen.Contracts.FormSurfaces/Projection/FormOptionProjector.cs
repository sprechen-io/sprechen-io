using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Wählt Optionen anhand von Werttyp und Validierungsregeln aus, damit die Oberfläche höchstens eine passende Optionsliste erhält.
/// </summary>
internal sealed class FormOptionProjector
{
    private readonly FormBooleanOptionProjector _booleanOptionProjector;
    private readonly FormEnumLikeOptionProjector _enumLikeOptionProjector;
    private readonly FormOneOfOptionProjector _oneOfOptionProjector;

    public FormOptionProjector()
        : this(
            new FormBooleanOptionProjector(),
            new FormEnumLikeOptionProjector(),
            new FormOneOfOptionProjector())
    {
    }

    internal FormOptionProjector(
        FormBooleanOptionProjector booleanOptionProjector,
        FormEnumLikeOptionProjector enumLikeOptionProjector,
        FormOneOfOptionProjector oneOfOptionProjector)
    {
        _booleanOptionProjector = booleanOptionProjector ?? throw new ArgumentNullException(nameof(booleanOptionProjector));
        _enumLikeOptionProjector = enumLikeOptionProjector ?? throw new ArgumentNullException(nameof(enumLikeOptionProjector));
        _oneOfOptionProjector = oneOfOptionProjector ?? throw new ArgumentNullException(nameof(oneOfOptionProjector));
    }

    public IReadOnlyList<FormSurfaceOption>? ProjectOptions(
        FormContract contract,
        string valueTypeKind,
        VariableDefinition? variable,
        FieldConstraints fieldConstraints,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(fieldConstraints);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }

        IReadOnlyList<FormSurfaceOption>? options = null;
        if (variable is not null
            && (string.Equals(valueTypeKind, ValueTypeKinds.Enum, StringComparison.OrdinalIgnoreCase)
                || string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase))
            && _enumLikeOptionProjector.TryProject(contract, variable.ValueType, requestedLanguage, politenessLevel, out var enumOptions))
        {
            options = enumOptions;
        }

        options ??= _oneOfOptionProjector.Project(fieldConstraints);

        if (string.Equals(valueTypeKind, ValueTypeKinds.Boolean, StringComparison.OrdinalIgnoreCase))
        {
            return _booleanOptionProjector.Project(requestedLanguage);
        }

        return options;
    }
}
