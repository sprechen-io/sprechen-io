using Microsoft.Extensions.Configuration;

namespace Sprechen.Forms.Extensions;

/// <summary>
/// Ermittelt die drei Host-Ports der Forms-App aus der Konfiguration, dann aus Umgebungsvariablen, sonst Standardwerte.
/// </summary>
internal static class FormsHostEndpointPortResolver
{
    public const string SectionName = "SprechenForms:HostEndpoints";
    public const string ApplicationPortKey = SectionName + ":ApplicationPort";
    public const string HealthPortKey = SectionName + ":HealthPort";
    public const string HttpsDevelopmentPortKey = SectionName + ":HttpsDevelopmentPort";
    public const string ApplicationPortEnvironmentVariable = "SprechenForms__HostEndpoints__ApplicationPort";
    public const string HealthPortEnvironmentVariable = "SprechenForms__HostEndpoints__HealthPort";
    public const string HttpsDevelopmentPortEnvironmentVariable = "SprechenForms__HostEndpoints__HttpsDevelopmentPort";

    private const int DefaultApplicationPort = 8100;
    private const int DefaultHealthPort = 8101;
    private const int DefaultHttpsDevelopmentPort = 55312;

    public static FormsHostEndpointPorts Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new FormsHostEndpointPorts(
            ApplicationPort: ResolvePort(configuration, ApplicationPortKey, ApplicationPortEnvironmentVariable, DefaultApplicationPort),
            HealthPort: ResolvePort(configuration, HealthPortKey, HealthPortEnvironmentVariable, DefaultHealthPort),
            HttpsDevelopmentPort: ResolvePort(configuration, HttpsDevelopmentPortKey, HttpsDevelopmentPortEnvironmentVariable, DefaultHttpsDevelopmentPort));
    }

    private static int ResolvePort(
        IConfiguration configuration,
        string configurationKey,
        string environmentVariable,
        int defaultValue)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentVariable);

        if (configuration.GetValue<int?>(configurationKey) is int configuredPort)
        {
            return configuredPort;
        }

        var environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
        return int.TryParse(environmentValue, out var environmentPort)
            ? environmentPort
            : defaultValue;
    }
}
