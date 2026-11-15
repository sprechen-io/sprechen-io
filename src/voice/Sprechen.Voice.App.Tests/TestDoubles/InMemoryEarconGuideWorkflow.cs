using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemoryEarconGuideWorkflow(IUserLanguageState userLanguageState) : IEarconGuideWorkflow
{
    public EarconGuideState GetEarconGuideState()
    {
        return GetEarconGuideState(userLanguageState.SelectedIsoCode);
    }

    public EarconGuideState GetEarconGuideState(string? preferredIsoCode)
    {
        var isoCode = InMemoryUserLanguageSettingsWorkflow.NormalizeIsoCode(preferredIsoCode);
        return new EarconGuideState(
            isoCode,
            ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconsIntro, isoCode),
            [
                new EarconGuideItem(
                    EarconId.FlowListening,
                    ResolveResourceText(VoiceInterfaceResourceIds.SessionSetupEarconFlowListening, isoCode))
            ]);
    }

    public ValueTask PreviewEarconAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask StopEarconPreviewsAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        if (VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text))
        {
            return text;
        }

        throw new InvalidOperationException($"Missing voice resource '{resourceId}' for '{isoCode}'.");
    }
}
