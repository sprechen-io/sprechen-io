using System.Security.Cryptography;
using System.Text;
using Json.Schema;
using Sprechen.Mda.SchemaBundle;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Speichert aufgebaute Schemabündel zwischen, damit unveränderte Schemainhalte für weitere Prüfungen wiederverwendet werden.
/// </summary>
internal sealed class SchemaBundleCache
{
    private string? _cachedSchemaBundleKey;
    private IReadOnlyDictionary<string, JsonSchema>? _cachedSchemaBundle;

    public IReadOnlyDictionary<string, JsonSchema> GetOrBuild(IReadOnlyDictionary<string, string> schemaTextsByFileName)
    {
        var cacheKey = BuildCacheKey(schemaTextsByFileName);
        if (_cachedSchemaBundle is not null
            && string.Equals(_cachedSchemaBundleKey, cacheKey, StringComparison.Ordinal))
        {
            return _cachedSchemaBundle;
        }

        _cachedSchemaBundle = new JsonSchemaBundleBuilder().Build(schemaTextsByFileName);
        _cachedSchemaBundleKey = cacheKey;
        return _cachedSchemaBundle;
    }

    private static string BuildCacheKey(IReadOnlyDictionary<string, string> schemaTextsByFileName)
    {
        return string.Join(
            '|',
            schemaTextsByFileName
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(BuildCacheKeyEntry));
    }

    private static string BuildCacheKeyEntry(KeyValuePair<string, string> pair)
    {
        var contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pair.Value)));
        return $"{pair.Key}:{contentHash}";
    }
}
