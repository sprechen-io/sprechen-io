using Microsoft.Extensions.Configuration;

namespace Sprechen.Contracts.Extensions;

/// <summary>
/// Löst Host-Ports aus der Konfiguration auf und ergänzt fehlende Werte, damit der Dienst mit festgelegten Standardports starten kann.
/// </summary>
internal sealed class ContractsHostEndpointSettingsResolver
{
    public const string SectionName = "SprechenContracts:HostEndpoints";
    public const string ApplicationPortKey = SectionName + ":ApplicationPort";
    public const string HealthPortKey = SectionName + ":HealthPort";
    public const string HttpsDevelopmentPortKey = SectionName + ":HttpsDevelopmentPort";
    public const string BindApplicationAndHealthToLoopbackKey =
        SectionName + ":BindApplicationAndHealthToLoopback";
    public const string ApplicationPortEnvironmentVariable =
        "SprechenContracts__HostEndpoints__ApplicationPort";
    public const string HealthPortEnvironmentVariable =
        "SprechenContracts__HostEndpoints__HealthPort";
    public const string HttpsDevelopmentPortEnvironmentVariable =
        "SprechenContracts__HostEndpoints__HttpsDevelopmentPort";
    public const string BindApplicationAndHealthToLoopbackEnvironmentVariable =
        "SprechenContracts__HostEndpoints__BindApplicationAndHealthToLoopback";

    private const int DefaultApplicationPort = 8090;
    private const int DefaultHealthPort = 8091;
    private const int DefaultHttpsDevelopmentPort = 55311;
    private readonly IConfiguration _configuration;

    public ContractsHostEndpointSettingsResolver(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public ContractsHostEndpointSettings Resolve()
    {
        return new ContractsHostEndpointSettings(
            ApplicationPort: _configuration.GetValue<int?>(ApplicationPortKey) ?? DefaultApplicationPort,
            HealthPort: _configuration.GetValue<int?>(HealthPortKey) ?? DefaultHealthPort,
            HttpsDevelopmentPort: _configuration.GetValue<int?>(HttpsDevelopmentPortKey) ?? DefaultHttpsDevelopmentPort,
            BindApplicationAndHealthToLoopback:
                _configuration.GetValue<bool?>(BindApplicationAndHealthToLoopbackKey) ?? false);
    }
}
