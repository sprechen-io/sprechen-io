using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class RecordingProfileSelector : IWakeWordDetectorProfileSelector
{
    private readonly WakeWordDetectorProfile _toReturn;

    public RecordingProfileSelector(WakeWordDetectorProfile toReturn)
    {
        _toReturn = toReturn;
    }

    public int CallCount { get; private set; }

    public IReadOnlyList<WakeWordDetectorProfile>? LastProfiles { get; private set; }

    public WakeWordDetectorProfileSelectionContext? LastContext { get; private set; }

    public WakeWordDetectorProfile Select(
        IReadOnlyList<WakeWordDetectorProfile> profiles,
        WakeWordDetectorProfileSelectionContext context)
    {
        CallCount++;
        LastProfiles = profiles;
        LastContext = context;
        return _toReturn;
    }
}
