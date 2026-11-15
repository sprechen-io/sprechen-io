using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sprechen.Service.Audit.Options;
using Sprechen.Service.Audit.Services;

namespace Sprechen.Service.Audit.Extensions;

/// <summary>
/// Registriert die Aufrufprotokollierung für Deepgram mit einer wirkungslosen Variante bei deaktivierter Aufzeichnung.
/// </summary>
public static class DeepgramAuditServiceCollectionExtensions
{
    /// <summary>
    /// Bindet und validiert die Optionen und wählt entsprechend IsEnabled die JSONL- oder die wirkungslose Implementierung.
    /// </summary>
    public static IServiceCollection AddDeepgramAudit(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DeepgramAuditOptions>()
            .Bind(configuration.GetSection(DeepgramAuditOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<JsonlDeepgramCallAuditSink>();
        services.AddSingleton<NoOpDeepgramCallAuditSink>();

        services.AddSingleton<IDeepgramCallAuditSink>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DeepgramAuditOptions>>().Value;
            return options.IsEnabled
                ? sp.GetRequiredService<JsonlDeepgramCallAuditSink>()
                : sp.GetRequiredService<NoOpDeepgramCallAuditSink>();
        });

        return services;
    }
}
