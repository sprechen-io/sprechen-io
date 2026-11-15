using Google.Protobuf.WellKnownTypes;
using Sprechen.Proto;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Lädt den unterstützten Sprachkatalog über gRPC, speichert ihn zwischen
/// und liefert bei jedem Abruf eine Kopie seiner Sprachliste zurück.
/// </summary>
internal sealed class SupportedLanguagesClient(SpeechGrpcClient languageClient) : ISupportedLanguagesClient
{
    private readonly SpeechGrpcClient _languageClient = languageClient ?? throw new ArgumentNullException(nameof(languageClient));
    private readonly AsyncCache<SupportedLanguages> _cache = new();

    public async Task<SupportedLanguages> GetAsync(CancellationToken ct)
    {
        var supportedLanguages = await _cache.GetOrCreateAsync(FetchAsync, ct).ConfigureAwait(false);
        return Clone(supportedLanguages);
    }

    private async Task<SupportedLanguages> FetchAsync(CancellationToken ct)
    {
        var response = await _languageClient.GetSupportedLanguagesAsync(new Empty(), cancellationToken: ct).ConfigureAwait(false);
        return SupportedLanguagesGrpcMapper.FromProto(response);
    }

    private static SupportedLanguages Clone(SupportedLanguages supportedLanguages)
    {
        return new SupportedLanguages(
            supportedLanguages.DefaultIsoCode,
            supportedLanguages.Languages.ToArray());
    }
}
