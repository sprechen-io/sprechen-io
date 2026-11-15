using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Koordiniert die Prüfung von Dokumentknoten und sammelt Fehler und Warnungen für den Composer.
/// </summary>
public sealed class SchemaValidator
{
    private readonly List<ValidationError> _errors = [];
    private readonly SchemaArrayValidator _arrayValidator = new();
    private readonly SchemaNumberValidator _numberValidator = new();
    private readonly SchemaObjectValidator _objectValidator = new();
    private readonly SchemaNodeValidationRouteResolver _routeResolver = new();
    private readonly SchemaStringValidator _stringValidator = new();
    private readonly SchemaUnionValidator _unionValidator = new();
    private readonly IReadOnlyDictionary<SchemaNodeValidationRoute, Action<SchemaNode, JsonNode, string>> _validators;

    public SchemaValidator()
    {
        _validators = new Dictionary<SchemaNodeValidationRoute, Action<SchemaNode, JsonNode, string>>
        {
            [SchemaNodeValidationRoute.Object] = (schema, node, path) =>
                _objectValidator.Validate(schema, node, path, _errors, ValidateNode),
            [SchemaNodeValidationRoute.Map] = (schema, node, path) =>
                _objectValidator.ValidateMap(schema, node, path, _errors, ValidateNode),
            [SchemaNodeValidationRoute.Array] = (schema, node, path) =>
                _arrayValidator.Validate(schema, node, path, _errors, ValidateNode),
            [SchemaNodeValidationRoute.String] = (schema, node, path) =>
                _stringValidator.Validate(schema, node, path, _errors),
            [SchemaNodeValidationRoute.Integer] = (schema, node, path) =>
                _numberValidator.ValidateInteger(schema, node, path, _errors),
            [SchemaNodeValidationRoute.Number] = (schema, node, path) =>
                _numberValidator.ValidateNumber(schema, node, path, _errors),
            [SchemaNodeValidationRoute.Boolean] = (_, node, path) => ValidateBoolean(node, path),
            [SchemaNodeValidationRoute.Union] = (schema, node, path) =>
                _unionValidator.Validate(schema, node, path, _errors),
        };
    }

    public IReadOnlyList<ValidationError> Validate(SchemaNode schema, JsonNode? document)
    {
        _errors.Clear();
        ValidateNode(schema, document, "$");
        return _errors.ToList();
    }

    private void ValidateNode(SchemaNode schema, JsonNode? node, string path)
    {
        if (node is null)
        {
            ValidateMissingNode(schema, path);
            ApplyNotBranchForNullValue(schema, path);
            return;
        }

        var route = _routeResolver.Resolve(schema);
        if (_validators.TryGetValue(route, out var validator))
        {
            validator(schema, node, path);
        }

        ApplyNotBranch(schema, node, path);
        ApplyConditionalBranch(schema, node, path);
    }

    private void ApplyNotBranch(SchemaNode schema, JsonNode node, string path)
    {
        if (schema.Not is null)
        {
            return;
        }

        if (new SchemaConditionalMatcher().Matches(schema.Not, node))
        {
            _errors.Add(new ValidationError(path, ValidationMessages.NotConstraintFailed));
        }
    }

    private void ApplyNotBranchForNullValue(SchemaNode schema, string path)
    {
        if (schema.Not is null)
        {
            return;
        }

        if (schema.Not.Type == SchemaNodeType.Null)
        {
            _errors.Add(new ValidationError(path, ValidationMessages.NotConstraintFailed));
        }
    }

    private void ApplyConditionalBranch(SchemaNode schema, JsonNode node, string path)
    {
        // allOf-Zweige können jeweils eigene Bedingungen enthalten. Deshalb werden
        // die Bedingungen der ursprünglichen Zweige einzeln geprüft, anstelle der
        // zusammengeführten Bedingung, die nur die letzte Definition enthält.
        if (schema.AllOf is { Count: > 0 } branches)
        {
            foreach (var branch in branches)
            {
                ApplyOneConditional(branch, node, path);
            }

            return;
        }

        ApplyOneConditional(schema, node, path);
    }

    private void ApplyOneConditional(SchemaNode schema, JsonNode node, string path)
    {
        if (schema.If is null)
        {
            return;
        }

        var matches = new SchemaConditionalMatcher().Matches(schema.If, node);
        var branch = matches ? schema.Then : schema.Else;
        if (branch is null)
        {
            return;
        }

        var errorsBefore = _errors.Count;
        ValidateNode(branch, node, path);

        for (var i = _errors.Count - 1; i >= errorsBefore; i--)
        {
            if (_errors[i].Severity != ValidationSeverity.Error)
            {
                _errors.RemoveAt(i);
            }
        }
    }

    private void ValidateMissingNode(SchemaNode schema, string path)
    {
        if (schema.IsRequired)
        {
            _errors.Add(new ValidationError(path, ValidationMessages.RequiredFieldMissing));
        }
    }

    private static void ValidateBoolean(JsonNode node, string path)
    {
        _ = path;
        if (node is not JsonValue value || !value.TryGetValue(out bool _))
        {
            // Für vorhandene Werte wird hier kein Fehler wegen eines abweichenden Booleschen Typs gemeldet.
        }
    }
}
