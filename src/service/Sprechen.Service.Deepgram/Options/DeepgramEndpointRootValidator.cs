namespace Sprechen.Service.Deepgram.Options;

/// <summary>
/// Prüft HTTPS-API-Wurzeln ohne Pfad, Abfrage oder Fragment. Für Spracherkennung ist
/// der EU-Endpunkt erforderlich. TTS erlaubt den globalen Endpunkt bei ausdrücklicher Freigabe.
/// </summary>
internal sealed class DeepgramEndpointRootValidator
{
    public void Validate(DeepgramOptions options, ICollection<string> failures)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(failures);

        if (!IsEuEndpointRoot(options.Endpoint))
        {
            failures.Add($"{DeepgramOptions.SectionName}:Endpoint must be the EU API root (https://api.eu.deepgram.com) without any path/query.");
        }

        if (options.TtsAllowNonEuEndpoint)
        {
            if (!IsSupportedEndpointRoot(options.TtsEndpoint))
            {
                failures.Add($"{DeepgramOptions.SectionName}:TtsEndpoint must be the API root (https://api.eu.deepgram.com or https://api.deepgram.com) without any path/query when TtsAllowNonEuEndpoint=true.");
            }
        }
        else if (!IsEuEndpointRoot(options.TtsEndpoint))
        {
            failures.Add($"{DeepgramOptions.SectionName}:TtsEndpoint must be the EU API root (https://api.eu.deepgram.com) without any path/query, or set TtsAllowNonEuEndpoint=true to opt out of EU-only TTS routing (GDPR posture).");
        }
    }

    private static bool IsEuEndpointRoot(string endpoint)
        => IsEndpointRoot(endpoint, static host => host.Equals("api.eu.deepgram.com", StringComparison.OrdinalIgnoreCase));

    private static bool IsSupportedEndpointRoot(string endpoint)
        => IsEndpointRoot(
            endpoint,
            static host => host.Equals("api.eu.deepgram.com", StringComparison.OrdinalIgnoreCase)
                || host.Equals("api.deepgram.com", StringComparison.OrdinalIgnoreCase));

    private static bool IsEndpointRoot(string endpoint, Func<string, bool> isSupportedHost)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && isSupportedHost(uri.Host)
            && uri.AbsolutePath is "/" or ""
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }
}
