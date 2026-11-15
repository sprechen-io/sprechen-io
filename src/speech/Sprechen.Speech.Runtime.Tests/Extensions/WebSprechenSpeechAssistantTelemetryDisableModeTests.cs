using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sprechen.Speech.App.ClientInfrastructure.Extensions;
using Sprechen.Speech.Runtime.Tests.Testing;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.VoiceAssistant;
using Sprechen.VoiceAssistant.Extensions;
using Sprechen.Web.SpeechClient;

namespace Sprechen.Speech.Runtime.Tests.Extensions;

public sealed class WebSprechenSpeechAssistantTelemetryDisableModeTests
{
    [Fact]
    public async Task EvaluationTelemetry_WhenEnabledByDefault_RequiresStudyIdentityBeforePersisting()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient()
            .AddWebSprechenSpeechAssistantSurfaces()
            .AddWebSprechenDialogs(BuildConfiguration(new Dictionary<string, string?>()));

        using var scope = ValidatedServiceProviderScope.Create(services);

        var options = scope.GetRequiredService<IOptions<EvaluationTelemetryOptions>>().Value;

        Assert.True(options.IsEnabled);
        Assert.True(options.RequireStudyContext);

        var telemetry = scope.GetRequiredService<IEvaluationSessionTelemetry>();
        _ = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        Assert.Empty(scope.GetRequiredService<InMemoryEvaluationTelemetryOutboxStore>().Checkpoints);
        Assert.Empty(scope.GetRequiredService<RecordingEvaluationTelemetryDeliveryTransport>().Bodies);
    }

    [Fact]
    public async Task EvaluationTelemetry_RequireStudyContext_false_persists_and_delivers()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["EvaluationTelemetry:RequireStudyContext"] = "false",
        });

        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient()
            .AddWebSprechenSpeechAssistantSurfaces()
            .AddWebSprechenDialogs(configuration);

        using var scope = ValidatedServiceProviderScope.Create(services);

        var telemetry = scope.GetRequiredService<IEvaluationSessionTelemetry>();
        _ = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        Assert.NotEmpty(scope.GetRequiredService<InMemoryEvaluationTelemetryOutboxStore>().Checkpoints);
        Assert.Single(scope.GetRequiredService<RecordingEvaluationTelemetryDeliveryTransport>().Bodies);
    }

    [Fact]
    public async Task EvaluationTelemetry_IsEnabled_false_does_not_create_a_delivery_checkpoint()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["EvaluationTelemetry:IsEnabled"] = "false",
        });

        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient()
            .AddWebSprechenSpeechAssistantSurfaces()
            .AddWebSprechenDialogs(configuration);

        using var scope = ValidatedServiceProviderScope.Create(services);

        var options = scope.GetRequiredService<IOptions<EvaluationTelemetryOptions>>().Value;

        Assert.False(options.IsEnabled);

        var telemetry = scope.GetRequiredService<IEvaluationSessionTelemetry>();
        _ = await telemetry.StartSessionAsync("test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);

        Assert.Empty(scope.GetRequiredService<InMemoryEvaluationTelemetryOutboxStore>().Checkpoints);
        Assert.Empty(scope.GetRequiredService<RecordingEvaluationTelemetryDeliveryTransport>().Bodies);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
    {
        var source = new MemoryConfigurationSource { InitialData = values };
        var builder = new ConfigurationBuilder();
        builder.Add(source);
        return builder.Build();
    }
}
