using System.Security.Cryptography;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Liest JSON-Artefakte und berechnet Inhalts-ETags, damit ungültiges JSON früh auffällt und HTTP-Antworten vergleichbar bleiben.
/// </summary>
internal sealed class MdaArtifactReader
{
    private readonly Func<string, IReadOnlyList<string>> _enumerateJsonFileNames;
    private readonly Func<string, IReadOnlyList<string>> _enumerateSubdirectories;
    private readonly Func<string, byte[]> _readAllBytes;

    public MdaArtifactReader()
        : this(MdaEmbeddedRuntimeArtifacts.ReadAllBytes, MdaEmbeddedRuntimeArtifacts.EnumerateJsonFileNames)
    {
    }

    internal MdaArtifactReader(
        Func<string, byte[]> readAllBytes,
        Func<string, IReadOnlyList<string>> enumerateJsonFileNames,
        Func<string, IReadOnlyList<string>>? enumerateSubdirectories = null)
    {
        _readAllBytes = readAllBytes ?? throw new ArgumentNullException(nameof(readAllBytes));
        _enumerateJsonFileNames = enumerateJsonFileNames ?? throw new ArgumentNullException(nameof(enumerateJsonFileNames));
        _enumerateSubdirectories = enumerateSubdirectories ?? MdaEmbeddedRuntimeArtifacts.EnumerateSubdirectories;
    }

    public MdaJsonArtifact ReadJsonArtifact(string path, string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var content = _readAllBytes(path);
        if (content.Length == 0)
        {
            throw new InvalidOperationException($"Embedded runtime artifact '{path}' is empty.");
        }

        return CreateJsonArtifact(content, contentType, $"artifact '{path}'");
    }

    public MdaJsonArtifact CreateJsonArtifact(byte[] content, string contentType, string errorContext)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorContext);

        if (content.Length == 0)
        {
            throw new InvalidOperationException($"Runtime {errorContext} is empty.");
        }

        try
        {
            using var _ = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON {errorContext}.", ex);
        }

        var hash = SHA256.HashData(content);
        var etag = $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";

        return new MdaJsonArtifact(contentType, content, etag);
    }

    public IReadOnlyList<string> EnumerateJsonFileNames(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        return _enumerateJsonFileNames(directoryPath).ToArray();
    }

    public IReadOnlyList<string> EnumerateSubdirectories(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        return _enumerateSubdirectories(directoryPath).ToArray();
    }
}
