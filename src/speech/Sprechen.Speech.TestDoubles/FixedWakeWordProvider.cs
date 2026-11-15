using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Liefert vorbereitete Aktivierungsformen unabhängig von der angefragten Sprache
/// und berücksichtigt einen bereits abgebrochenen Aufrufertoken.
/// </summary>
public sealed class FixedWakeWordProvider(
    string[] wakeWordActivationForms,
    EarconId? notRecognizedEarconId = null,
    WakeUtteranceWindow? wakeUtteranceWindow = null) : IWakeWordProvider
{
    private readonly string[] _wakeWordActivationForms = wakeWordActivationForms ?? throw new ArgumentNullException(nameof(wakeWordActivationForms));
    private readonly EarconId? _notRecognizedEarconId = notRecognizedEarconId;
    private readonly WakeUtteranceWindow? _wakeUtteranceWindow = wakeUtteranceWindow;

    public Task<WakeWordInventory> GetWakeWordInventoryAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new WakeWordInventory(
            _wakeWordActivationForms,
            [],
            _notRecognizedEarconId,
            _wakeUtteranceWindow));
    }
}
