using Sprechen.ServiceDefaults;
using Sprechen.Service.Audit.Extensions;
using Sprechen.Service.Grpc;
using Sprechen.Service.Security;
using Sprechen.Service.Options;
using Sprechen.Service.Core;
using Sprechen.Service.Deepgram;
using Microsoft.Extensions.Options;

namespace Sprechen.Service.Extensions;

/// <summary>
/// Registriert die Dienste und Endpunkte des Sprachservers einschließlich Authentifizierung und CORS.
/// </summary>
internal static class HostExtensions
{
    private const string CorsPolicyName = "ConfiguredCors";

    public static void AddSprechenService(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.SetupKestrel();

        var configuration = builder.Configuration;
        builder.AddSprechenServiceDefaults();

        builder.Services
            .AddLanguageCore(configuration)
            .AddDeepgramSpeech(configuration)
            .AddDeepgramAudit(configuration)
            .AddRecognizedAudioDumpCleanup();

        builder.Services.AddOptions<ServiceAuthOptions>()
            .Bind(configuration.GetSection(ServiceAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<AudioPacingOptions>()
            .Bind(configuration.GetSection(AudioPacingOptions.SectionName));

        builder.Services.AddSingleton<IValidateOptions<ConversationStreamingOptions>, ConversationStreamingOptionsSelectionValidator>();
        builder.Services.AddOptions<ConversationStreamingOptions>()
            .Bind(configuration.GetSection(ConversationStreamingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<LanguageResolver>();
        builder.Services.AddSingleton<SpeechRequestValidator>();
        builder.Services.AddSingleton<SpeechRecognitionDiagnosticsEmitter>();
        builder.Services.AddSingleton<SpeechSynthesisDiagnosticsEmitter>();
        builder.Services.AddSingleton<ProviderExceptionToRpcMapper>();
        builder.Services.AddSingleton<SpeechToTextTranscriptStreamWriter>();
        builder.Services.AddSingleton<SpeechConversationEventStreamWriter>();
        builder.Services.AddSingleton<TextToSpeechAudioStreamWriter>();
        builder.Services.AddServiceApplication();
        builder.Services.AddSingleton<SpeechToTextEndpoint>();
        builder.Services.AddSingleton<SpeechToTextStreamEndpoint>();
        builder.Services.AddSingleton<SpeechToConversationStreamEndpoint>();
        builder.Services.AddSingleton<TextToSpeechToStreamEndpoint>();

        builder.Services.AddGrpc(options => { options.Interceptors.Add<ServiceApiKeyInterceptor>(); });
        builder.Services.AddGrpcReflection();

        AddConfiguredCors(builder.Services, builder.Configuration, builder.Environment);
    }

    public static void UseSprechenService(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseCors(CorsPolicyName);
        app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

        app.MapGrpcService<SpeechGrpcService>()
            .EnableGrpcWeb()
            .RequireCors(CorsPolicyName);

        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        app.MapSprechenDefaultEndpoints();

        var displayVersion = app.Services
            .GetRequiredService<IApplicationVersionInfoProvider>()
            .GetCurrent()
            .DisplayVersion;
        app.MapGet("/", () => $"gRPC Sprachservice online (version: {displayVersion})");
    }

    private static void AddConfiguredCors(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var normalizedOrigins = allowedOrigins
            .Select(origin => origin?.Trim())
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var allowAnyOrigin = Array.Exists(normalizedOrigins, origin => string.Equals(origin, "*", StringComparison.Ordinal));

        if (allowAnyOrigin && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins cannot contain '*' outside Development. Configure explicit origins for deployed environments.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod();

                if (allowAnyOrigin)
                {
                    policy.AllowAnyOrigin();
                    return;
                }

                if (normalizedOrigins.Length > 0)
                {
                    policy.WithOrigins(normalizedOrigins);
                }
            });
        });
    }
}
