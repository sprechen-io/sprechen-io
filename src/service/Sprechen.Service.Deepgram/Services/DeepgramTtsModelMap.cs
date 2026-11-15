using Microsoft.Extensions.Options;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Ordnet normalisierten Sprachcodes konfigurierte TTS-Modelle zu.
/// Fehlende Zuordnungen werden beim Modellabruf abgelehnt.
/// </summary>
internal sealed class DeepgramTtsModelMap
{
    private readonly IReadOnlyDictionary<string, string> _modelsByIsoCode;

    public DeepgramTtsModelMap(IOptions<DeepgramOptions> options)
    {
        var ttsModels = options?.Value?.TtsModels ?? throw new ArgumentNullException(nameof(options));

        var modelsByIsoCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ttsModels)
        {
            var iso = LanguageCodeNormalizer.NormalizeIso6391(entry.Key);
            if (iso is null)
            {
                continue;
            }

            var model = entry.Value?.Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                continue;
            }

            modelsByIsoCode[iso] = model;
        }

        _modelsByIsoCode = modelsByIsoCode;
    }

    public string GetModel(string languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        var iso = LanguageCodeNormalizer.NormalizeIso6391(languageCode);
        if (iso is null)
        {
            throw new InvalidOperationException($"Invalid ISO 639-1 language code '{languageCode}'.");
        }

        if (_modelsByIsoCode.TryGetValue(iso, out var model) && !string.IsNullOrWhiteSpace(model))
        {
            return model;
        }

        throw new InvalidOperationException(
            $"Deepgram TTS model for language '{iso}' is not configured. Set {DeepgramOptions.SectionName}:TtsModels:{iso}.");
    }
}

