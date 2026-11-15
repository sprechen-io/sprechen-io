namespace Sprechen.Contracts.Extensions;

/// <summary>
/// Bündelt Ports und Adressbindung des Hosts, damit Kestrel eine gemeinsam aufgelöste Konfiguration erhält.
/// </summary>
internal sealed record ContractsHostEndpointSettings(
    int ApplicationPort,
    int HealthPort,
    int HttpsDevelopmentPort,
    bool BindApplicationAndHealthToLoopback);
