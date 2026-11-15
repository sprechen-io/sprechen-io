using Microsoft.Extensions.Options;

namespace Sprechen.Service.Options;

/// <summary>
/// Prüft die zusätzliche Obergrenze von acht Sekunden für das Inaktivitätszeitlimit im Deepgram-Betrieb.
/// </summary>
internal sealed class ConversationStreamingOptionsSelectionValidator : IValidateOptions<ConversationStreamingOptions>
{
    internal const int DeepgramSafeMaximumClientInactivityTimeoutMs = 8_000;

    public ValidateOptionsResult Validate(string? name, ConversationStreamingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ClientInactivityTimeoutMs <= DeepgramSafeMaximumClientInactivityTimeoutMs)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            $"{ConversationStreamingOptions.SectionName}:ClientInactivityTimeoutMs must be less than or equal to " +
            $"{DeepgramSafeMaximumClientInactivityTimeoutMs} for the Deepgram runtime " +
            "so the service closes the Deepgram live stream before the provider idle timeout.");
    }
}
