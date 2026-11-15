using Json.Schema;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt Artefakte in einen gemeinsamen Index, damit doppelte Schlüssel über Verzeichnisgrenzen hinweg erkannt werden.
/// </summary>
internal sealed class MdaReferenceArtifactSetLoader
{
    private readonly MdaArtifactReader _artifactReader;
    private readonly MdaReferenceJsonModelReader _modelReader;

    public MdaReferenceArtifactSetLoader(MdaArtifactReader artifactReader, MdaReferenceJsonModelReader modelReader)
    {
        _artifactReader = artifactReader ?? throw new ArgumentNullException(nameof(artifactReader));
        _modelReader = modelReader ?? throw new ArgumentNullException(nameof(modelReader));
    }

    public ArtifactSet<TKey, TModel> Load<TKey, TModel>(
        string directoryPath,
        JsonSchema schema,
        Func<TModel, TKey> getKey,
        Func<TKey, string> duplicateMessage)
        where TKey : notnull
        where TModel : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        return Load([directoryPath], schema, getKey, duplicateMessage);
    }

    /// <summary>
    /// Führt Artefakte aus dem neutralen Verzeichnis und den Anwendungsverzeichnissen zusammen. Leere Einzelverzeichnisse sind erlaubt. Der Gesamtbestand muss gefüllt sein, sofern <paramref name="allowEmpty"/> nicht gesetzt ist.
    /// </summary>
    public ArtifactSet<TKey, TModel> LoadCategory<TKey, TModel>(
        string neutralDirectoryPath,
        string applicationCategoryName,
        JsonSchema schema,
        Func<TModel, TKey> getKey,
        Func<TKey, string> duplicateMessage,
        bool allowEmpty = false)
        where TKey : notnull
        where TModel : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(neutralDirectoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationCategoryName);

        return Load(ResolveCategoryDirectories(neutralDirectoryPath, applicationCategoryName), schema, getKey, duplicateMessage, allowEmpty);
    }

    private IReadOnlyList<string> ResolveCategoryDirectories(string neutralDirectoryPath, string applicationCategoryName)
    {
        var directoryPaths = new List<string>();
        if (_artifactReader.EnumerateJsonFileNames(neutralDirectoryPath).Count > 0)
        {
            directoryPaths.Add(neutralDirectoryPath);
        }

        foreach (var distribution in _artifactReader.EnumerateSubdirectories(MdaContractsArtifactPaths.ApplicationsRootDirectoryPath))
        {
            var categoryDirectoryPath = $"{MdaContractsArtifactPaths.ApplicationsRootDirectoryPath}/{distribution}/{applicationCategoryName}";
            if (_artifactReader.EnumerateJsonFileNames(categoryDirectoryPath).Count > 0)
            {
                directoryPaths.Add(categoryDirectoryPath);
            }
        }

        return directoryPaths;
    }

    public ArtifactSet<TKey, TModel> Load<TKey, TModel>(
        IReadOnlyList<string> directoryPaths,
        JsonSchema schema,
        Func<TModel, TKey> getKey,
        Func<TKey, string> duplicateMessage,
        bool allowEmpty = false)
        where TKey : notnull
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(directoryPaths);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(getKey);
        ArgumentNullException.ThrowIfNull(duplicateMessage);

        var artifacts = new Dictionary<TKey, MdaJsonArtifact>();
        var models = new Dictionary<TKey, TModel>();
        foreach (var directoryPath in directoryPaths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

            foreach (var fileName in _artifactReader.EnumerateJsonFileNames(directoryPath))
            {
                var path = $"{directoryPath}/{fileName}";
                var raw = _artifactReader.ReadJsonArtifact(path, contentType: "application/json");
                var model = _modelReader.Read<TModel>(schema, raw, path);
                var key = getKey(model);

                if (!artifacts.TryAdd(key, raw))
                {
                    throw new InvalidOperationException(duplicateMessage(key));
                }

                models.Add(key, model);
            }
        }

        // Artefakte können vollständig in Anwendungsverzeichnissen liegen. Das neutrale
        // Verzeichnis darf deshalb leer sein. Nur die vereinigte Menge muss Artefakte
        // enthalten. AllowEmpty erlaubt auch einen leeren Gesamtbestand.
        if (artifacts.Count == 0 && !allowEmpty)
        {
            throw new InvalidOperationException(
                $"Runtime artifact directory set [{string.Join(", ", directoryPaths)}] does not contain JSON artifacts.");
        }

        return new ArtifactSet<TKey, TModel>(artifacts, models);
    }
}
