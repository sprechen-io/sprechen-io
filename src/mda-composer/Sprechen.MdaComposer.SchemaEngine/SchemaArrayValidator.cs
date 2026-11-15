using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft Arraygröße, Einträge und verlangte Eindeutigkeit, um Fehler an den jeweiligen Dokumentpositionen zu melden.
/// </summary>
internal sealed class SchemaArrayValidator
{
    private readonly SchemaJsonValueClassifier _valueClassifier = new();

    public void Validate(
        SchemaNode schema,
        JsonNode node,
        string path,
        ICollection<ValidationError> errors,
        Action<SchemaNode, JsonNode?, string> validateNode)
    {
        if (node is not JsonArray arr)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.ExpectedArrayGot, _valueClassifier.GetDisplayName(node))));
            return;
        }

        ValidateArraySize(schema, arr, path, errors);
        ValidateItems(schema, arr, path, validateNode);
        ValidateUniqueness(schema, arr, path, errors);
    }

    private static void ValidateUniqueness(
        SchemaNode schema,
        JsonArray arr,
        string path,
        ICollection<ValidationError> errors)
    {
        if (schema.UniqueItems != true)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < arr.Count; i++)
        {
            var canonical = arr[i]?.ToJsonString() ?? "null";
            if (!seen.Add(canonical))
            {
                errors.Add(new ValidationError(
                    $"{path}[{i}]",
                    string.Format(ValidationMessages.DuplicateArrayItem, canonical)));
            }
        }
    }

    private static void ValidateArraySize(
        SchemaNode schema,
        JsonArray arr,
        string path,
        ICollection<ValidationError> errors)
    {
        if (schema.MinItems is not null && arr.Count < schema.MinItems)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.MinItems, schema.MinItems, arr.Count)));
        }

        if (schema.MaxItems is not null && arr.Count > schema.MaxItems)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.MaxItems, schema.MaxItems, arr.Count)));
        }
    }

    private static void ValidateItems(
        SchemaNode schema,
        JsonArray arr,
        string path,
        Action<SchemaNode, JsonNode?, string> validateNode)
    {
        if (schema.Items is null)
        {
            return;
        }

        for (var i = 0; i < arr.Count; i++)
        {
            validateNode(schema.Items, arr[i], $"{path}[{i}]");
        }
    }
}
