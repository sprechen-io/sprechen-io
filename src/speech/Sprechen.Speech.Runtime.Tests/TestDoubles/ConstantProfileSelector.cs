using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class ConstantProfileSelector : IWakeWordDetectorProfileSelector
{
    private readonly WakeWordDetectorProfile _toReturn;

    public ConstantProfileSelector(WakeWordDetectorProfile toReturn)
    {
        _toReturn = toReturn;
    }

    public WakeWordDetectorProfile Select(
        IReadOnlyList<WakeWordDetectorProfile> profiles,
        WakeWordDetectorProfileSelectionContext context)
    {
        return _toReturn;
    }
}
