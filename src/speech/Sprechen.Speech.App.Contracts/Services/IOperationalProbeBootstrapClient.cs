namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Holt über die Hostgrenze einen neuen Kontext für den betrieblichen Testlauf.</summary>
public interface IOperationalProbeBootstrapClient
{
    Task<OperationalProbeContext> BootstrapAsync(CancellationToken cancellationToken);
}
