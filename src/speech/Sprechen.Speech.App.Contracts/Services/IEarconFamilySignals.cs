
namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verteilt Änderungen der aktiven Klangfamilie und stellt ihren aktuellen Zustand bereit.
/// </summary>
public interface IEarconFamilySignals
{
    EarconFamily? ActiveFamily { get; }

    ValueTask SetActiveFamilyAsync(EarconFamily? activeFamily);

    IDisposable SubscribeToActiveFamilyChanged(Func<EarconFamilyChanged, ValueTask> handler);
}
