using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Ordnet Werttypen und Formathinweisen HTML-Eingabetypen zu, damit der Browser passende Eingabefunktionen anbieten kann.
/// </summary>
internal sealed class FormBaseInputControlMapper
{
    private readonly FormFormatHintInputControlMapper _formatHintMapper;

    public FormBaseInputControlMapper(FormFormatHintInputControlMapper formatHintMapper)
    {
        _formatHintMapper = formatHintMapper ?? throw new ArgumentNullException(nameof(formatHintMapper));
    }

    public string Map(string valueTypeKind, VariableDefinition? variable)
    {
        if (string.Equals(valueTypeKind, ValueTypeKinds.Integer, StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Number;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Date, StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Date;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Time, StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Time;
        }

        var formatHintInput = _formatHintMapper.TryMap(variable);
        if (!string.IsNullOrWhiteSpace(formatHintInput))
        {
            return formatHintInput;
        }

        return string.Equals(variable?.SemanticTag, "contact.email", StringComparison.OrdinalIgnoreCase)
            ? FormInputControlTypes.Email
            : FormInputControlTypes.Text;
    }
}
