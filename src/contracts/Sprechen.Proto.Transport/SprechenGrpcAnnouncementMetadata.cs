using Grpc.Core;

namespace Sprechen.Proto.Transport;

/// <summary>
/// Überträgt die Ansageart als gRPC-Metadatum, damit sie ohne Änderung der Nachricht protokolliert werden kann.
/// </summary>
/// <remarks>Beim Lesen werden unbekannte Werte auf die allgemeine Ansageart zurückgeführt.</remarks>
public static class SprechenGrpcAnnouncementMetadata
{
    public const string AnnouncementKindHeaderName = "x-sprechen-announcement-kind";

    /// <summary>Standardkategorie für fehlende oder unbekannte Ansagearten beim Lesen.</summary>
    public const string PromptKind = "prompt";

    /// <summary>Kennzeichnet eine Ansage, dass keine Spracheingabe gehört wurde.</summary>
    public const string NoInputKind = "noinput";

    /// <summary>Kennzeichnet eine Ansage zu einem behebbaren Erkennungs- oder Eingabefehler.</summary>
    public const string RecoverableFailureKind = "recoverablefailure";

    /// <summary>
    /// Begrenzt die beim Lesen akzeptierten Ansagearten, damit unbekannte Headerwerte nicht als eigene Kategorien verwendet werden.
    /// </summary>
    private static readonly HashSet<string> KnownAnnouncementKinds =
        new(StringComparer.Ordinal) { PromptKind, NoInputKind, RecoverableFailureKind };

    public static Metadata CreateHeaders(string announcementKind)
    {
        var headers = new Metadata();
        headers.Add(AnnouncementKindHeaderName, Normalize(announcementKind));
        return headers;
    }

    /// <summary>
    /// Liest die Ansageart und verwendet bei fehlenden oder unbekannten Werten die Standardkategorie.
    /// </summary>
    public static string ReadAnnouncementKind(Metadata? headers)
    {
        var declared = Normalize(headers?.GetValue(AnnouncementKindHeaderName));
        return KnownAnnouncementKinds.Contains(declared) ? declared : PromptKind;
    }

    private static string Normalize(string? announcementKind)
        => string.IsNullOrWhiteSpace(announcementKind)
            ? PromptKind
            : announcementKind.Trim().ToLowerInvariant();
}
