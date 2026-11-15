namespace Sprechen.Contracts.Services;

/// <summary>
/// Hält Inhalt, Medientyp und ETag gemeinsam, damit HTTP-Antwort und Cacheprüfung dieselbe Artefaktfassung verwenden können.
/// </summary>
internal sealed record MdaJsonArtifact(
    string ContentType,
    byte[] Content,
    string ETag);


