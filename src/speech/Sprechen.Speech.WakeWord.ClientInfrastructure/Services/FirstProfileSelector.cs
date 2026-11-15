using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Wählt das erste Detektorprofil des Katalogs als Standard und verwirft leere Listen,
/// damit die Konfigurationsauflösung immer mit einem vorhandenen Profil weiterarbeitet.
/// </summary>
internal sealed class FirstProfileSelector : IWakeWordDetectorProfileSelector
{
    public WakeWordDetectorProfile Select(
        IReadOnlyList<WakeWordDetectorProfile> profiles,
        WakeWordDetectorProfileSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(context);

        if (profiles.Count == 0)
        {
            throw new ArgumentException(
                "Wake-word detector profile list must not be empty.",
                nameof(profiles));
        }

        return profiles[0];
    }
}
