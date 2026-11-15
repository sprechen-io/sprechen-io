using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft eine Schema-Bedingung anhand von Fehlern, damit Warnungen die Auswahl des zutreffenden Zweigs nicht beeinflussen.
/// </summary>
internal sealed class SchemaConditionalMatcher
{
    public bool Matches(SchemaNode ifSchema, JsonNode? document)
    {
        var validator = new SchemaValidator();
        var errors = validator.Validate(ifSchema, document);
        return !errors.Any(static error => error.Severity == ValidationSeverity.Error);
    }
}
