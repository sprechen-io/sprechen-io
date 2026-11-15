using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft Zahlentypen sowie Unter- und Obergrenzen, um numerische Fehler mit Dokumentposition zu melden.
/// </summary>
internal sealed class SchemaNumberValidator
{
    private readonly SchemaJsonValueClassifier _valueClassifier = new();

    public void ValidateInteger(
        SchemaNode schema,
        JsonNode node,
        string path,
        ICollection<ValidationError> errors)
    {
        if (node is not JsonValue value || !value.TryGetValue(out int intValue))
        {
            ValidateIntegerCompatibleNumber(schema, node, path, errors);
            return;
        }

        ValidateNumericRange(schema, intValue, path, errors);
    }

    public void ValidateNumber(
        SchemaNode schema,
        JsonNode node,
        string path,
        ICollection<ValidationError> errors)
    {
        if (node is not JsonValue value)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.ExpectedNumberGot, _valueClassifier.GetDisplayName(node))));
            return;
        }

        if (value.TryGetValue(out double doubleValue))
        {
            ValidateNumericRange(schema, doubleValue, path, errors);
            return;
        }

        if (value.TryGetValue(out int intValue))
        {
            ValidateNumericRange(schema, intValue, path, errors);
            return;
        }

        errors.Add(new ValidationError(path, ValidationMessages.InvalidNumber));
    }

    private void ValidateIntegerCompatibleNumber(
        SchemaNode schema,
        JsonNode node,
        string path,
        ICollection<ValidationError> errors)
    {
        if (node is JsonValue value && value.TryGetValue(out double doubleValue) && doubleValue == Math.Floor(doubleValue))
        {
            ValidateNumericRange(schema, doubleValue, path, errors);
            return;
        }

        errors.Add(new ValidationError(path,
            string.Format(ValidationMessages.ExpectedIntegerGot, _valueClassifier.GetDisplayName(node))));
    }

    private static void ValidateNumericRange(
        SchemaNode schema,
        double value,
        string path,
        ICollection<ValidationError> errors)
    {
        if (schema.Minimum is not null && value < schema.Minimum)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.MinValue, schema.Minimum, value)));
        }

        if (schema.Maximum is not null && value > schema.Maximum)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.MaxValue, schema.Maximum, value)));
        }
    }
}
