using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Lädt Schemas und Referenzverträge per HTTP für den Composer und protokolliert Fehler einzelner Lade- und Inspektionsschritte.
/// </summary>
public sealed class SchemaTextLoader : ISchemaTextProvider
{
    private readonly HttpClient _http;
    private readonly IReadOnlyList<ComposerRootSchemaInfo> _allowedCatalog;
    private readonly SchemaIntrospector _introspector = new();
    private readonly EnumRegistry _enumRegistry = new();
    private readonly SchemaArtifactsBag _artifacts = new();
    private string _cacheBuster = $"v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    private bool _loaded;

    public SchemaTextLoader(HttpClient http)
        : this(http, options: null)
    {
    }

    public SchemaTextLoader(HttpClient http, ComposerOptions? options)
    {
        _http = http;
        _allowedCatalog = ComposerRootCatalog.GetForOptions(options ?? new ComposerOptions());
    }

    public SchemaIntrospector Introspector => _introspector;
    public EnumRegistry EnumRegistry => _enumRegistry;
    public bool IsLoaded => _loaded;

    public IReadOnlyDictionary<string, SchemaNode> ParsedSchemas => _artifacts.ParsedSchemas;
    public IReadOnlyDictionary<string, string> RawSchemas => _artifacts.RawSchemas;
    public IReadOnlyList<ReferenceContract> ReferenceContracts => _artifacts.ReferenceContracts;

    public IReadOnlyDictionary<string, string> GetSchemaTexts() => _artifacts.RawSchemas;

    public async Task LoadAllAsync()
    {
        if (_loaded) return;
        await LoadCoreAsync();
    }

    public async Task ReloadAsync()
    {
        _artifacts.Clear();
        _loaded = false;
        _cacheBuster = $"v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        await LoadCoreAsync();
    }

    private async Task LoadCoreAsync()
    {
        var schemaFiles = await LoadManifestEntriesAsync("manifests/schemas.txt");
        foreach (var schemaFile in schemaFiles)
        {
            if (!IsAllowed(schemaFile))
            {
                continue;
            }
            try
            {
                var json = await _http.GetStringAsync($"schemas/{schemaFile}?{_cacheBuster}");
                _artifacts.StoreRawSchema(schemaFile, json);
                _introspector.RegisterSchema(schemaFile, json);
            }
            catch (HttpRequestException ex)
            {
                LogLoadFailure("Fetch schema", schemaFile, ex);
            }
        }

        foreach (var schemaFile in _artifacts.RawSchemas.Keys.OrderBy(static file => file, StringComparer.OrdinalIgnoreCase).ToList())
        {
            try
            {
                var node = _introspector.Introspect(schemaFile);
                _artifacts.StoreParsedSchema(schemaFile, node);
                _enumRegistry.ExtractFromSchema(schemaFile, node);
            }
            catch (Exception ex)
            {
                LogLoadFailure("Introspect schema", schemaFile, ex);
            }
        }

        await LoadReferenceContractsAsync();
        _loaded = true;
    }

    private async Task LoadReferenceContractsAsync()
    {
        var referenceContractFiles = await LoadManifestEntriesAsync("manifests/reference-contracts.txt");
        foreach (var fileName in referenceContractFiles)
        {
            var rootSchema = ComposerRootCatalog.MatchReferenceContract(fileName, _allowedCatalog);
            if (rootSchema is null)
            {
                continue;
            }

            try
            {
                var json = await _http.GetStringAsync($"reference-contracts/{fileName}?{_cacheBuster}");
                _artifacts.AddReferenceContract(new ReferenceContract(fileName, rootSchema.FileName, json));
            }
            catch (HttpRequestException ex)
            {
                LogLoadFailure("Fetch reference contract", fileName, ex);
            }
        }
    }

    private bool IsAllowed(string schemaFile)
    {
        return _allowedCatalog.Any(
                schema => schema.FileName.Equals(schemaFile, StringComparison.OrdinalIgnoreCase))
            || !IsKnownCatalogSchema(schemaFile);
    }

    private static bool IsKnownCatalogSchema(string schemaFile)
        => ComposerRootCatalog.All.Any(
            schema => schema.FileName.Equals(schemaFile, StringComparison.OrdinalIgnoreCase));

    private async Task<IReadOnlyList<string>> LoadManifestEntriesAsync(string manifestPath)
    {
        try
        {
            var content = await _http.GetStringAsync($"{manifestPath}?{_cacheBuster}");
            return content
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static entry => entry, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            LogLoadFailure("Fetch manifest", manifestPath, ex);
            return [];
        }
    }

    private static void LogLoadFailure(string action, string file, Exception ex)
    {
        Console.Error.WriteLine(
            $"[SchemaTextLoader] {action} failed for '{file}': {ex.GetType().Name}: {ex.Message}");
    }
}
