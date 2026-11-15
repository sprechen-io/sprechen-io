using Microsoft.Extensions.DependencyInjection;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Speech.App.ClientInfrastructure.Extensions;
using Sprechen.Speech.Runtime.Tests.Testing;
using Sprechen.Web.FormSurfaces;
using Sprechen.Web.SpeechClient;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Extensions;

public sealed class WebSprechenSpeechAssistantSurfaceServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebSprechenSpeechAssistantSurfaces_WhenComposedWithCore_ValidatesSurfaceProjectionGraph()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient()
            .AddWebSprechenSpeechAssistantSurfaces();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var formSurfaceDefinitionBuilder = scope.GetRequiredService<IFormSurfaceDefinitionBuilder>();
        var formSurfacePresenter = scope.GetRequiredService<IFormSurfacePresenter>();
        var speechCaptureSurfaceStateStore = scope.GetRequiredService<SpeechCaptureSurfaceStateStore>();
        var speechCaptureSurfaceState = scope.GetRequiredService<ISpeechCaptureSurfaceState>();
        var formSurfaceStore = scope.GetRequiredService<SpeechAssistantFormSurfaceStore>();
        var formSurfaceState = scope.GetRequiredService<ISpeechAssistantFormSurfaceState>();
        var formSurfaceController = scope.GetRequiredService<ISpeechAssistantFormSurfaceController>();

        Assert.IsType<FormSurfaceDefinitionBuilder>(formSurfaceDefinitionBuilder);
        Assert.IsType<FormSurfacePresenter>(formSurfacePresenter);
        Assert.Same(speechCaptureSurfaceStateStore, speechCaptureSurfaceState);
        Assert.Same(formSurfaceStore, formSurfaceState);
        Assert.Same(formSurfaceStore, formSurfaceController);
    }

    [Fact]
    public void AddWebSprechenSpeechAssistantSurfaceControllers_WhenComposedWithSharedRoots_ValidatesSurfaceControllerGraph()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient()
            .AddWebSprechenSpeechAssistantSurfaces()
            .AddWebSprechenDialogs()
            .AddWebSprechenSpeechAssistantSurfaceControllers();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var speechCaptureSurfaceController = scope.GetRequiredService<ISpeechCaptureSurfaceController>();
        var speechCaptureSurfacePressController = scope.GetRequiredService<ISpeechCaptureSurfacePressController>();
        var sessionController = scope.GetRequiredService<SpeechAssistantSessionController>();
        var sessionControllerContract = scope.GetRequiredService<ISpeechAssistantSessionController>();

        Assert.IsType<SpeechCaptureSurfaceController>(speechCaptureSurfaceController);
        Assert.IsType<SpeechCaptureSurfaceController>(speechCaptureSurfacePressController);
        Assert.Same(speechCaptureSurfaceController, speechCaptureSurfacePressController);
        Assert.Same(sessionController, sessionControllerContract);
    }
}
