namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class FixedWakeWordProvider(params string[] activationForms) : IWakeWordProvider
{
    private readonly string[] _activationForms = activationForms ?? throw new ArgumentNullException(nameof(activationForms));

    public Task<WakeWordInventory> GetWakeWordInventoryAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        return Task.FromResult(new WakeWordInventory(_activationForms, []));
    }
}
