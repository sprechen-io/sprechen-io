using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft Objekt- und Map-Eigenschaften nach dem Schema, um Fehler an ihren Dokumentpositionen zu melden.
/// </summary>
internal sealed class SchemaObjectValidator
{
    private readonly SchemaJsonValueClassifier _valueClassifier = new();

    public void Validate(
        SchemaNode schema,
        JsonNode node,
        string path,
        ICollection<ValidationError> errors,
        Action<SchemaNode, JsonNode?, string> validateNode)
    {
        if (node is not JsonObject obj)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.ExpectedObjectGot, _valueClassifier.GetDisplayName(node))));
            return;
        }

        ValidateProperties(schema, obj, path, errors, validateNode);
        ValidateObjectSize(schema, obj, path, errors);
        ValidatePropertyNames(schema, obj, path, errors, validateNode);
    }

    public void ValidateMap(
        SchemaNode schema,
        JsonNode node,
        string path,
        ICollection<ValidationError> errors,
        Action<SchemaNode, JsonNode?, string> validateNode)
    {
        if (node is not JsonObject obj)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.ExpectedMapGot, _valueClassifier.GetDisplayName(node))));
            return;
        }

        if (schema.AdditionalProperties is null)
        {
            return;
        }

        foreach (var (key, value) in obj)
        {
            validateNode(schema.AdditionalProperties, value, $"{path}[\"{key}\"]");
        }

        ValidatePropertyNames(schema, obj, path, errors, validateNode);
    }

    private static void ValidateProperties(
        SchemaNode schema,
        JsonObject obj,
        string path,
        ICollection<ValidationError> errors,
        Action<SchemaNode, JsonNode?, string> validateNode)
    {
        if (schema.Properties is null)
        {
            return;
        }

        foreach (var prop in schema.Properties)
        {
            ValidateProperty(prop, obj, path, errors, validateNode);
        }

        ValidateUnknownProperties(schema, obj, path, errors);
    }

    private static void ValidateProperty(
        SchemaNode prop,
        JsonObject obj,
        string path,
        ICollection<ValidationError> errors,
        Action<SchemaNode, JsonNode?, string> validateNode)
    {
        var propPath = $"{path}.{prop.Name}";
        if (obj.ContainsKey(prop.Name))
        {
            validateNode(prop, obj[prop.Name], propPath);
            return;
        }

        if (prop.IsRequired)
        {
            errors.Add(new ValidationError(propPath,
                string.Format(ValidationMessages.RequiredPropertyMissing, prop.Name)));
        }
    }

    private static void ValidateUnknownProperties(
        SchemaNode schema,
        JsonObject obj,
        string path,
        ICollection<ValidationError> errors)
    {
        var knownProps = schema.Properties!.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var severity = schema.UnevaluatedProperties == false
            ? ValidationSeverity.Error
            : ValidationSeverity.Warning;
        foreach (var key in obj.Select(p => p.Key))
        {
            if (!knownProps.Contains(key) && schema.AdditionalProperties is null)
            {
                errors.Add(new ValidationError($"{path}.{key}",
                    string.Format(ValidationMessages.UnknownProperty, key), severity));
            }
        }
    }

    private static void ValidatePropertyNames(
        SchemaNode schema,
        JsonObject obj,
        string path,
        ICollection<ValidationError> errors,
        Action<SchemaNode, JsonNode?, string> validateNode)
    {
        if (schema.PropertyNames is null)
        {
            return;
        }

        foreach (var key in obj.Select(p => p.Key))
        {
            var keyNode = JsonValue.Create(key);
            validateNode(schema.PropertyNames, keyNode, $"{path}.#{key}");
        }
    }

    private static void ValidateObjectSize(
        SchemaNode schema,
        JsonObject obj,
        string path,
        ICollection<ValidationError> errors)
    {
        if (schema.MinProperties is not null && obj.Count < schema.MinProperties)
        {
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.MinProperties, schema.MinProperties, obj.Count)));
        }
    }
}
