using System.ComponentModel.DataAnnotations;

namespace Sprechen.Service.Deepgram;

/// <summary>
/// Konfiguration der Deepgram-Endpunkte, Modelle, Erkennungsbegriffe und Zeitgrenzen.
/// </summary>
internal sealed class DeepgramOptions
{
    public const string SectionName = "Deepgram";

    private const int DEFAULT_TTS_MAX_CONCURRENCY = 1;
    private const int DEFAULT_TTS_RATE_LIMIT_MAX_RETRIES = 10;
    private const int DEFAULT_TTS_RATE_LIMIT_INITIAL_DELAY_MS = 500;
    private const int DEFAULT_TTS_RATE_LIMIT_MAX_DELAY_MS = 30_000;

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required, Url]
    public string Endpoint { get; init; } = "https://api.eu.deepgram.com";

    [Required, Url]
    public string TtsEndpoint { get; init; } = "https://api.eu.deepgram.com";

    public bool TtsAllowNonEuEndpoint { get; init; }

    [Required]
    public string SttModel { get; init; } = "nova-3-general";

    /// <summary>
    /// Ergänzt im Gesprächsprofil die modellierten Befehlsbegriffe der Anfragesprache. Standardwert ist true.
    /// </summary>
    public bool EnableCommandKeyterms { get; init; } = true;

    /// <summary>
    /// Ergänzt im Gesprächsprofil Aktivierungswörter und den sprachabhängigen Aktivierungsnamen. Standardwert ist true.
    /// </summary>
    public bool EnableWakeWordKeyterms { get; init; } = true;

    /// <summary>
    /// Ergänzt im Gesprächsprofil die Bestätigungs- und Verneinungsbegriffe der Anfragesprache. Standardwert ist true.
    /// </summary>
    public bool EnableConfirmationKeyterms { get; init; } = true;

    public IReadOnlyDictionary<string, string> TtsModels { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Änderungen dieser Stimmen erfordern eine erneute Prüfung des Lautstärkeausgleichs beim Client.
            ["de"] = "aura-2-aurelia-de",
            ["en"] = "aura-2-iris-en",
            ["fr"] = "aura-2-agathe-fr",
            ["es"] = "aura-2-silvia-es",
            ["it"] = "aura-2-maia-it"
        };

    [Range(8_000, 48_000)]
    public int TtsSampleRateHz { get; init; } = 24_000;

    [Range(1, 25)]
    public int TtsMaxConcurrency { get; init; } = DEFAULT_TTS_MAX_CONCURRENCY;

    [Range(0, 20)]
    public int TtsRateLimitMaxRetries { get; init; } = DEFAULT_TTS_RATE_LIMIT_MAX_RETRIES;

    [Range(0, 60_000)]
    public int TtsRateLimitInitialDelayMs { get; init; } = DEFAULT_TTS_RATE_LIMIT_INITIAL_DELAY_MS;

    [Range(0, 300_000)]
    public int TtsRateLimitMaxDelayMs { get; init; } = DEFAULT_TTS_RATE_LIMIT_MAX_DELAY_MS;

    /// <summary>
    /// Maximale Wartezeit nach Finalize auf WebSocket-Close und abschließende Transkripte.
    /// Die Wartezeit endet vorzeitig, sobald die Verbindung ihren Abschluss meldet.
    /// </summary>
    public TimeSpan LiveFinalizeWaitTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximale Wartezeit beim Beenden der Verbindung über Stop oder ersatzweise Close.
    /// </summary>
    public TimeSpan LiveStopTimeout { get; init; } = TimeSpan.FromSeconds(2);
}
