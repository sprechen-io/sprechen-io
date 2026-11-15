using System.ComponentModel.DataAnnotations;

namespace Sprechen.Service.Options;

/// <summary>
/// Konfiguriert das Zeitlimit ohne neue Audiodaten in einem Gesprächseingangsstrom.
/// </summary>
internal sealed class ConversationStreamingOptions
{
    public const string SectionName = "ConversationStreaming";

    private const int DefaultClientInactivityTimeoutMs = 8_000;

    [Range(1_000, 30_000)]
    public int ClientInactivityTimeoutMs { get; init; } = DefaultClientInactivityTimeoutMs;
}
