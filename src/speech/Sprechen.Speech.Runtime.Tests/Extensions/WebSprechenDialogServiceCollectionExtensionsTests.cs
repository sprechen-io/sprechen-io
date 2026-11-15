using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.ClientInfrastructure.Extensions;
using Sprechen.VoiceAssistant.Extensions;
using Sprechen.Speech.Runtime.Tests.Testing;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.SpeechClient;

namespace Sprechen.Speech.Runtime.Tests.Extensions;

public sealed class WebSprechenDialogServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebSprechenDialogs_WhenComposedWithSharedClientRoots_ValidatesDialogGraphs()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient()
            .AddWebSprechenSpeechAssistantSurfaces()
            .AddWebSprechenDialogs();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var systemClock = scope.GetRequiredService<ISystemClock>();
        var studyTelemetryContext = scope.GetRequiredService<IStudyTelemetryContext>();
        var evaluationTelemetryOutboxStore = scope.GetRequiredService<IEvaluationTelemetryOutboxStore>();
        var evaluationTelemetryDeliveryTransport = scope.GetRequiredService<IEvaluationTelemetryDeliveryTransport>();
        var evaluationSessionTelemetry = scope.GetRequiredService<IEvaluationSessionTelemetry>();
        var evaluationFormTelemetry = scope.GetRequiredService<IEvaluationFormTelemetry>();
        var evaluationCommandTelemetry = scope.GetRequiredService<IEvaluationCommandTelemetry>();
        var evaluationTelemetry = scope.GetRequiredService<IEvaluationTelemetry>();
        var voiceCommandDialog = scope.GetRequiredService<IVoiceCommandDialog>();
        var speechAssistantService = scope.GetRequiredService<ISpeechAssistantService>();
        var silentInputController = scope.GetRequiredService<ISpeechAssistantSilentInputController>();

        Assert.IsType<SystemClock>(systemClock);
        Assert.IsType<StudyTelemetryContext>(studyTelemetryContext);
        Assert.IsType<InMemoryEvaluationTelemetryOutboxStore>(evaluationTelemetryOutboxStore);
        Assert.IsType<RecordingEvaluationTelemetryDeliveryTransport>(evaluationTelemetryDeliveryTransport);
        Assert.IsType<EvaluationSessionTelemetry>(evaluationSessionTelemetry);
        Assert.IsType<EvaluationFormTelemetry>(evaluationFormTelemetry);
        Assert.IsType<EvaluationCommandTelemetry>(evaluationCommandTelemetry);
        Assert.IsType<EvaluationTelemetry>(evaluationTelemetry);
        Assert.IsType<FormVoiceCommandDialog>(voiceCommandDialog);
        Assert.IsType<SpeechAssistantService>(speechAssistantService);
        Assert.Same(speechAssistantService, silentInputController);
    }
}
