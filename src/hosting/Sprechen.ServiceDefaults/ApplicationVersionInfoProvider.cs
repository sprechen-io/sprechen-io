using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Löst Version und Quellenrevision bei seiner Erstellung aus Konfiguration und Assembly auf,
/// damit spätere Abfragen denselben Stand liefern.
/// </summary>
internal sealed class ApplicationVersionInfoProvider : IApplicationVersionInfoProvider
{
    private readonly ApplicationVersionInfo _current;

    public ApplicationVersionInfoProvider(
        IOptions<ApplicationVersionOptions> applicationOptions,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        var applicationAssembly = ResolveApplicationAssembly(hostEnvironment);
        var configuredVersion = Normalize(applicationOptions.Value.Version);
        var configuredSourceRevision = Normalize(applicationOptions.Value.SourceRevision);
        var informationalVersion = Normalize(
                applicationAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion)
            ?? "1.0.0";
        var assemblyVersion = Normalize(applicationAssembly.GetName().Version?.ToString()) ?? informationalVersion;
        var sourceRevision = configuredSourceRevision ?? TryExtractSourceRevision(informationalVersion);
        var displayVersion = configuredVersion ?? StripMetadataSuffix(informationalVersion) ?? assemblyVersion;

        _current = new ApplicationVersionInfo(
            displayVersion,
            informationalVersion,
            assemblyVersion,
            sourceRevision,
            ShortenSourceRevision(sourceRevision));
    }

    public ApplicationVersionInfo GetCurrent() => _current;

    private static Assembly ResolveApplicationAssembly(IHostEnvironment hostEnvironment)
    {
        var applicationName = Normalize(hostEnvironment.ApplicationName);
        if (applicationName is not null)
        {
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(
                    assembly.GetName().Name,
                    applicationName,
                    StringComparison.Ordinal));
            if (loadedAssembly is not null)
            {
                return loadedAssembly;
            }

            try
            {
                return Assembly.Load(new AssemblyName(applicationName));
            }
            catch
            {
                // Einstiegsassembly unten, wenn der konfigurierte Name nicht geladen werden kann.
            }
        }

        return Assembly.GetEntryAssembly() ?? typeof(ApplicationVersionInfoProvider).Assembly;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? StripMetadataSuffix(string? informationalVersion)
    {
        var normalized = Normalize(informationalVersion);
        if (normalized is null)
        {
            return null;
        }

        var metadataSeparatorIndex = normalized.IndexOf('+', StringComparison.Ordinal);
        return metadataSeparatorIndex <= 0
            ? normalized
            : normalized[..metadataSeparatorIndex];
    }

    private static string? TryExtractSourceRevision(string? informationalVersion)
    {
        var normalized = Normalize(informationalVersion);
        if (normalized is null)
        {
            return null;
        }

        var metadataSeparatorIndex = normalized.IndexOf('+', StringComparison.Ordinal);
        if (metadataSeparatorIndex < 0 || metadataSeparatorIndex == normalized.Length - 1)
        {
            return null;
        }

        return Normalize(normalized[(metadataSeparatorIndex + 1)..]);
    }

    private static string? ShortenSourceRevision(string? sourceRevision)
    {
        var normalized = Normalize(sourceRevision);
        if (normalized is null)
        {
            return null;
        }

        return normalized.Length <= 8
            ? normalized
            : normalized[..8];
    }
}
