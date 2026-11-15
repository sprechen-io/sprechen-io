using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Sprechen.Service.Core.Services;

/// <summary>
/// Meldet konfigurierte Sprachdienstanbieter und die aufgelösten Implementierungstypen.
/// Die Prüfung führt keinen Netzwerkaufruf zum Anbieter aus.
/// </summary>
internal sealed class SpeechProviderHealthCheck : IHealthCheck
{
    private readonly ISpeechToTextProvider _sttProvider;
    private readonly ITextToSpeechProvider _ttsProvider;
    private readonly SpeechProviderOptions _options;

    public SpeechProviderHealthCheck(
        ISpeechToTextProvider sttProvider,
        ITextToSpeechProvider ttsProvider,
        IOptions<SpeechProviderOptions> options)
    {
        _sttProvider = sttProvider ?? throw new ArgumentNullException(nameof(sttProvider));
        _ttsProvider = ttsProvider ?? throw new ArgumentNullException(nameof(ttsProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["sttProvider"] = _options.SttProvider.ToString(),
            ["ttsProvider"] = _options.TtsProvider.ToString(),
            ["sttPort"] = _sttProvider.GetType().FullName ?? _sttProvider.GetType().Name,
            ["ttsPort"] = _ttsProvider.GetType().FullName ?? _ttsProvider.GetType().Name
        };

        var description = $"STT {_options.SttProvider}, TTS {_options.TtsProvider}.";
        return Task.FromResult(HealthCheckResult.Healthy(description, data));
    }
}
