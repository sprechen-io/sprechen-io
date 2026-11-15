using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Meldet Werte, die zu keiner angebotenen Schemavariante passen,
/// damit der Editor solche ungültigen Werte erkennt.
/// </summary>
internal sealed class SchemaUnionValidator
{
    private readonly SchemaUnionMatchEvaluator _unionMatchEvaluator = new();

    public void Validate(
        SchemaNode schema,
        JsonNode node,
        string path,
        ICollection<ValidationError> errors)
    {
        var options = _unionMatchEvaluator.GetOptions(schema);
        if (options.Count == 0)
        {
            return;
        }

        if (!_unionMatchEvaluator.HasMatchingOption(schema, node))
        {
            var unionType = schema.Type == SchemaNodeType.OneOf ? "oneOf" : "anyOf";
            errors.Add(new ValidationError(path,
                string.Format(ValidationMessages.UnionNoMatch, options.Count, unionType)));
        }
    }
}
