namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Fasst <c>allOf</c>-Zweige für die Schemaanzeige zusammen und erhält die ursprünglichen Zweige zur weiteren Prüfung.
/// </summary>
internal sealed class SchemaAllOfMerger
{
    public SchemaNode Merge(
        IReadOnlyList<SchemaNode> branches,
        string name,
        bool isRequired)
    {
        if (branches.Count == 0)
        {
            return new SchemaNode
            {
                Name = name,
                Type = SchemaNodeType.Unknown,
                IsRequired = isRequired,
                Description = "Empty allOf composition",
            };
        }

        var mergedType = ResolveMergedType(branches);
        var mergedProperties = MergeProperties(branches);
        var mergedItems = branches.Select(static b => b.Items).LastOrDefault(static items => items is not null);
        var mergedAdditionalProperties = branches
            .Select(static b => b.AdditionalProperties)
            .LastOrDefault(static a => a is not null);

        return new SchemaNode
        {
            Name = name,
            Type = mergedType,
            IsRequired = isRequired,
            Title = PreferLatestNonNull(branches, static b => b.Title),
            Description = PreferLatestNonNull(branches, static b => b.Description),
            EnumValues = PreferLatestNonNull(branches, static b => b.EnumValues),
            Properties = mergedProperties,
            Items = mergedItems,
            AdditionalProperties = mergedAdditionalProperties,
            DefaultValue = PreferLatestNonNull(branches, static b => b.DefaultValue),
            Minimum = PreferLatestNonNull(branches, static b => b.Minimum),
            Maximum = PreferLatestNonNull(branches, static b => b.Maximum),
            MinLength = PreferLatestNonNull(branches, static b => b.MinLength),
            MaxLength = PreferLatestNonNull(branches, static b => b.MaxLength),
            Pattern = PreferLatestNonNull(branches, static b => b.Pattern),
            ConstValue = PreferLatestNonNull(branches, static b => b.ConstValue),
            Format = PreferLatestNonNull(branches, static b => b.Format),
            MinItems = PreferLatestNonNull(branches, static b => b.MinItems),
            MaxItems = PreferLatestNonNull(branches, static b => b.MaxItems),
            MinProperties = PreferLatestNonNull(branches, static b => b.MinProperties),
            If = PreferLatestNonNull(branches, static b => b.If),
            Then = PreferLatestNonNull(branches, static b => b.Then),
            Else = PreferLatestNonNull(branches, static b => b.Else),
            AllOf = branches,
            IsMap = mergedAdditionalProperties is not null && (mergedProperties is null or { Count: 0 }),
        };
    }

    private static SchemaNodeType ResolveMergedType(IReadOnlyList<SchemaNode> branches)
    {
        var concreteTypes = branches
            .Select(static b => b.Type)
            .Where(static t => t != SchemaNodeType.Unknown)
            .Distinct()
            .ToList();

        if (concreteTypes.Count == 0)
        {
            return SchemaNodeType.Unknown;
        }

        if (concreteTypes.Count == 1)
        {
            return concreteTypes[0];
        }

        return SchemaNodeType.Unknown;
    }

    private static IReadOnlyList<SchemaNode>? MergeProperties(IReadOnlyList<SchemaNode> branches)
    {
        var requiredNames = branches
            .Where(static b => b.Properties is not null)
            .SelectMany(static b => b.Properties!)
            .Where(static p => p.IsRequired)
            .Select(static p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        var merged = new Dictionary<string, SchemaNode>(StringComparer.Ordinal);
        foreach (var branch in branches)
        {
            if (branch.Properties is null)
            {
                continue;
            }

            foreach (var property in branch.Properties)
            {
                merged[property.Name] = property with
                {
                    IsRequired = requiredNames.Contains(property.Name),
                };
            }
        }

        return merged.Count == 0 ? null : merged.Values.ToList();
    }

    private static T? PreferLatestNonNull<T>(
        IReadOnlyList<SchemaNode> branches,
        Func<SchemaNode, T?> selector)
        where T : class
    {
        for (var i = branches.Count - 1; i >= 0; i--)
        {
            var value = selector(branches[i]);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static T? PreferLatestNonNull<T>(
        IReadOnlyList<SchemaNode> branches,
        Func<SchemaNode, T?> selector)
        where T : struct
    {
        for (var i = branches.Count - 1; i >= 0; i--)
        {
            var value = selector(branches[i]);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }
}
