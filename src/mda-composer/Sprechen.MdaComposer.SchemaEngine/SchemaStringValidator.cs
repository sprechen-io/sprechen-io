using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft Textdarstellungen auf Konstanten, Enum-Werte, Länge und Muster, um Validierungsfehler am Dokument zu melden.
/// </summary>
internal sealed class SchemaStringValidator
{
    private static readonly TimeSpan PatternMatchTimeout = TimeSpan.FromMilliseconds(100);

    private readonly SchemaJsonValueClassifier _valueClassifier = new();

    public void Validate(SchemaNode schema, JsonNode node, string path, ICollection<ValidationError> errors)
    {
        if (node is not JsonValue val)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.ExpectedStringGot, _valueClassifier.GetDisplayName(node))));
            return;
        }

        var str = val.ToString();

        if (schema.ConstValue is not null && str != schema.ConstValue)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.ConstExpected, schema.ConstValue, str)));
            return;
        }

        if (schema.EnumValues is { Count: > 0 } && !schema.EnumValues.Contains(str))
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.EnumNotAllowed, str, string.Join(", ", schema.EnumValues))));
            return;
        }

        ValidateLength(schema, str, path, errors);
        ValidatePattern(schema, str, path, errors);
    }

    private void ValidateLength(SchemaNode schema, string value, string path, ICollection<ValidationError> errors)
    {
        if (schema.MinLength is not null && value.Length < schema.MinLength)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.MinLength, schema.MinLength, value.Length)));
        }

        if (schema.MaxLength is not null && value.Length > schema.MaxLength)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.MaxLength, schema.MaxLength, value.Length)));
        }
    }

    private void ValidatePattern(SchemaNode schema, string value, string path, ICollection<ValidationError> errors)
    {
        if (schema.Pattern is null)
        {
            return;
        }

        Regex regex;
        try
        {
            regex = new Regex(schema.Pattern, RegexOptions.None, PatternMatchTimeout);
        }
        catch (RegexParseException)
        {
            // Bei ungültiger Regex-Syntax entfällt die Musterprüfung. Die übrigen Zeichenkettenregeln bleiben davon unberührt.
            return;
        }

        try
        {
            if (!regex.IsMatch(value))
            {
                errors.Add(new ValidationError(path,
                    string.Format(ValidationMessages.PatternMismatch, schema.Pattern)));
            }
        }
        catch (RegexMatchTimeoutException)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.PatternTimeout, schema.Pattern)));
        }
    }

}
