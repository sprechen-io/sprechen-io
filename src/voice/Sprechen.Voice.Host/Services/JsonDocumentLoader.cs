using System.Text.Json;
using Microsoft.Extensions.FileProviders;

namespace Sprechen.Voice.Host.Services;

/// <summary>
/// Liest JSON aus dem Host-Dateianbieter, damit Kataloge nicht an physische Pfade
/// gebunden sind.
/// </summary>
internal static class JsonDocumentLoader
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T Load<T>(IFileProvider fileProvider, string resourceFilePath, string resourceType)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(fileProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        var fileInfo = fileProvider.GetFileInfo(resourceFilePath);
        if (!fileInfo.Exists)
        {
            throw new InvalidOperationException($"{resourceType} '{resourceFilePath}' was not found.");
        }

        try
        {
            using var stream = fileInfo.CreateReadStream();
            var document = JsonSerializer.Deserialize<T>(stream, DefaultJsonOptions);
            if (document is null)
            {
                throw new InvalidOperationException($"{resourceType} '{resourceFilePath}' is empty.");
            }

            return document;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{resourceType} '{resourceFilePath}' contains invalid JSON.", ex);
        }
    }

    public static T Load<T>(IFileProvider fileProvider, string resourceFilePath)
        where T : class
    {
        return Load<T>(fileProvider, resourceFilePath, "Resource file");
    }
}
