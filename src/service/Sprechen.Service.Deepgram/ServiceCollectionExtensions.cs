using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.Authenticate.v1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprechen.Service.Deepgram.Options;
using Sprechen.Service.Deepgram.Services;

namespace Sprechen.Service.Deepgram;

/// <summary>
/// Registriert Deepgram-Clients, Adapter und Konfigurationsprüfungen.
/// </summary>
public static class DeepgramServiceCollectionExtensions
{
    private const string RecognizedAudioDumpDirectoryVariable = "SPRECHEN_RECOGNIZED_AUDIO_DUMP_DIR";

    public static IServiceCollection AddDeepgramSpeech(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<DeepgramOptions>, OptionsSelectionValidator>();

        var deepgramOptionsBuilder = services.AddOptions<DeepgramOptions>()
            .Bind(configuration.GetSection(DeepgramOptions.SectionName));

        if (IsDeepgramSelected(configuration))
        {
            // Die Deepgram-Konfiguration ist nur erforderlich, wenn Deepgram für STT oder TTS ausgewählt ist.
            deepgramOptionsBuilder.ValidateOnStart();
        }

        services.AddSingleton<IListenRESTClient>(sp =>
        {
            DeepgramSdkInitializer.EnsureInitialized();
            var opts = sp.GetRequiredService<IOptions<DeepgramOptions>>().Value;
            var clientOptions = new DeepgramHttpClientOptions(apiKey: opts.ApiKey, baseAddress: NormalizeEndpointRoot(opts.Endpoint));
            return ClientFactory.CreateListenRESTClient(apiKey: opts.ApiKey, options: clientOptions);
        });

        services.AddSingleton<IDeepgramListenWebSocketClientFactory, DeepgramListenWebSocketClientFactory>();
        services.AddSingleton<IDeepgramSpeakWebSocketClientFactory, DeepgramSpeakWebSocketClientFactory>();
        services.AddSingleton<DeepgramTtsModelMap>();

        services.AddSingleton<DeepgramPrerecordedTranscriptionClient>(sp =>
            new DeepgramPrerecordedTranscriptionClient(
                sp.GetRequiredService<IListenRESTClient>(),
                sp.GetRequiredService<IOptions<DeepgramOptions>>(),
                sp.GetRequiredService<ILogger<DeepgramPrerecordedTranscriptionClient>>(),
                ReadRecognizedAudioDumpDirectory()));

        services.AddSingleton<DeepgramSpeechToTextAdapter>();
        services.AddSingleton<DeepgramTextToSpeechAdapter>();
        services.AddSingleton<ISpeechToTextProvider>(sp => sp.GetRequiredService<DeepgramSpeechToTextAdapter>());
        services.AddSingleton<IConversationalSpeechToTextProvider>(sp => sp.GetRequiredService<DeepgramSpeechToTextAdapter>());
        services.AddSingleton<ITextToSpeechProvider>(sp => sp.GetRequiredService<DeepgramTextToSpeechAdapter>());

        return services;
    }

    public static IServiceCollection AddRecognizedAudioDumpCleanup(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService(sp => new RecognizedAudioDumpCleanup(
            ReadRecognizedAudioDumpDirectory(),
            sp.GetRequiredService<ILogger<RecognizedAudioDumpCleanup>>()));

        return services;
    }

    private static string? ReadRecognizedAudioDumpDirectory()
    {
        var configured = Environment.GetEnvironmentVariable(RecognizedAudioDumpDirectoryVariable);
        return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();
    }

    private static bool IsDeepgramSelected(IConfiguration configuration)
    {
        var providerOptions = new SpeechProviderOptions();
        configuration.GetSection(SpeechProviderOptions.SectionName).Bind(providerOptions);

        return providerOptions.SttProvider == SpeechProvider.Deepgram
            || providerOptions.TtsProvider == SpeechProvider.Deepgram;
    }

    private static string NormalizeEndpointRoot(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Deepgram endpoint '{endpoint}' is not a valid absolute URI.");
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
