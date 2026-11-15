namespace Sprechen.Forms.Extensions;

/// <summary>
/// Hält die drei aufgelösten Endpunkt-Ports der Forms-App: Anwendung, Health und HTTPS-Entwicklung.
/// </summary>
internal sealed record FormsHostEndpointPorts(
    int ApplicationPort,
    int HealthPort,
    int HttpsDevelopmentPort);
