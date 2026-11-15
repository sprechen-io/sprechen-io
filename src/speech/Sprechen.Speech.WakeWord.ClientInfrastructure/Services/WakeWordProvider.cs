using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Liefert benutzerdefinierte oder modellierte Aktivierungsformen mit Rückmeldesignalen
/// und Äußerungsfenster, damit die Erkennung ein zusammengehöriges Inventar erhält.
/// </summary>
internal sealed class WakeWordProvider(
    IVoiceActivationSettings voiceActivationSettings,
    ILogger<WakeWordProvider> logger) : IWakeWordProvider
{
    private static readonly string[] EmptyActivationForms = [];

    private readonly IVoiceActivationSettings _voiceActivationSettings =
        voiceActivationSettings ?? throw new ArgumentNullException(nameof(voiceActivationSettings));

    private readonly ILogger<WakeWordProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<WakeWordInventory> GetWakeWordInventoryAsync(string isoCode, CancellationToken cancellationToken)
    {
        var normalizedIsoCode = IsoCode.NormalizeIso6391(isoCode);

        try
        {
            var overrideActivationForms = await _voiceActivationSettings.GetWakeWordActivationFormsOverrideAsync(cancellationToken).ConfigureAwait(false);
            var normalizedOverride = new WakeWordActivationFormCodec().Normalize(overrideActivationForms);
            if (normalizedOverride.Length > 0)
            {
                return new WakeWordInventory(
                    normalizedOverride,
                    EmptyActivationForms,
                    VoiceInterfaceModelCatalog.WakeWord.GetRecoveryNotRecognizedEarconId(),
                    VoiceInterfaceModelCatalog.WakeWord.GetUtteranceWindow())
                {
                    ActivatedEarconId = VoiceInterfaceModelCatalog.WakeWord.GetActivationEarconId(),
                };
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load wake word overrides.");
        }

        return new WakeWordInventory(
            VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(normalizedIsoCode).ToArray(),
            VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage(normalizedIsoCode).ToArray(),
            VoiceInterfaceModelCatalog.WakeWord.GetRecoveryNotRecognizedEarconId(),
            VoiceInterfaceModelCatalog.WakeWord.GetUtteranceWindow())
        {
            ActivatedEarconId = VoiceInterfaceModelCatalog.WakeWord.GetActivationEarconId(),
        };
    }
}
