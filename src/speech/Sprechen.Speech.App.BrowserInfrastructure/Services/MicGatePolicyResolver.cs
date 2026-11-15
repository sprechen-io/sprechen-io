using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Liest <c>micGate.policy</c> aus <see cref="SpeechCaptureProfileCatalogReader"/>,
/// damit die Mikrofonsperre dem Modell folgt. Ein fehlender Block oder unbekannter Wert
/// wird als <see cref="MicGatePolicy.Heuristic"/> aufgelöst.
/// </summary>
internal sealed class MicGatePolicyResolver : IMicGatePolicyResolver
{
    private readonly Lazy<MicGatePolicy> _policy;

    public MicGatePolicyResolver()
    {
        _policy = new Lazy<MicGatePolicy>(ResolveFromCatalog);
    }

    public MicGatePolicy GetPolicy() => _policy.Value;

    private static MicGatePolicy ResolveFromCatalog()
    {
        var profile = SpeechCaptureProfileCatalogReader.GetMicGateProfile();
        if (profile is null)
        {
            return MicGatePolicy.Heuristic;
        }

        return profile.Policy switch
        {
            "hardOff" => MicGatePolicy.HardOff,
            "heuristic" => MicGatePolicy.Heuristic,
            _ => MicGatePolicy.Heuristic,
        };
    }
}
