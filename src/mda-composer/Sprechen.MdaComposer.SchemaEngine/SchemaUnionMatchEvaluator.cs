using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft, ob mindestens eine Schemavariante ohne Validierungsmeldung zum Wert passt,
/// um Reparaturbedarf zu erkennen.
/// </summary>
internal sealed class SchemaUnionMatchEvaluator
{
    public IReadOnlyList<SchemaNode> GetOptions(SchemaNode schema)
    {
        return schema.OneOf ?? schema.AnyOf ?? [];
    }

    public bool HasMatchingOption(SchemaNode schema, JsonNode node)
    {
        foreach (var option in GetOptions(schema))
        {
            var validator = new SchemaValidator();
            if (validator.Validate(option, node).Count == 0)
            {
                return true;
            }
        }

        return false;
    }
}
