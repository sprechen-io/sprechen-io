namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erstellt und zerlegt JSON-Pointer, damit Editoren Pfadsegmente einheitlich maskieren und auflösen.
/// </summary>
public static class JsonPointer
{
    public const string Root = "";

    public static string Append(string pointer, string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        var escapedSegment = segment
            .Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);

        return string.IsNullOrEmpty(pointer)
            ? $"/{escapedSegment}"
            : $"{pointer}/{escapedSegment}";
    }

    public static string AppendIndex(string pointer, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return Append(pointer, index.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static IReadOnlyList<string> Parse(string pointer)
    {
        if (string.IsNullOrEmpty(pointer))
        {
            return [];
        }

        if (!pointer.StartsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException("JSON pointer must be empty or start with '/'.", nameof(pointer));
        }

        return pointer
            .Split('/', StringSplitOptions.None)
            .Skip(1)
            .Select(static segment => segment
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal))
            .ToList();
    }
}
