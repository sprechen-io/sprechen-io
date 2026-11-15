using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Registriert die Clients und ihre Abhängigkeiten gemeinsam, damit Aufrufer geladene Verträge auch prüfen lassen.
/// </summary>
public static class ContractsApiClientServiceCollectionExtensions
{
    public static IServiceCollection AddContractsApiClient(
        this IServiceCollection services,
        string clientName,
        Action<IServiceProvider, HttpClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentNullException.ThrowIfNull(configureClient);

        services.AddHttpClient(clientName, configureClient);
        services.AddScoped(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return new ContractsApiHttpStreamFetcher(httpClientFactory.CreateClient(clientName));
        });
        services.AddScoped<ContractsApiResourceRoutes>();
        services.AddScoped<ContractsApiModelSchemaRoutes>();
        services.AddScoped<ContractsApiResourceStreamClient>();
        services.AddScoped<ContractsApiModelSchemaStreamClient>();
        services.AddScoped<ContractsApiJsonStreamReader>();
        services.AddScoped<ContractsApiPreflightFailureMapper>();
        services.AddScoped<ContractsApiResourceCacheKeyFactory>();
        services.AddScoped<FormsCatalogProfileReferenceCollector>();
        services.AddScoped<FormsCatalogDocumentResourceClient>();
        services.AddScoped<FormContractDocumentResourceClient>();
        services.AddScoped<FieldDefinitionsResourceClient>();
        services.AddScoped<VariableDefinitionsResourceClient>();
        services.AddScoped<FormVoiceProfileResourceClient>();
        services.AddScoped<ApplicationSelectionDialogResourceClient>();
        services.AddScoped<ApplicationSelectionDialogProfileResourceClient>();
        services.AddScoped(serviceProvider => new FormsCatalogTypedClient(
            serviceProvider.GetRequiredService<FormsCatalogDocumentResourceClient>(),
            serviceProvider.GetRequiredService<ApplicationSelectionDialogResourceClient>(),
            serviceProvider.GetRequiredService<ApplicationSelectionDialogProfileResourceClient>(),
            serviceProvider.GetRequiredService<FormsCatalogProfileReferenceCollector>(),
            serviceProvider.GetRequiredService<ContractsApiPreflightFailureMapper>()));
        services.AddScoped(serviceProvider => new MdaFormContractTypedClient(
            serviceProvider.GetRequiredService<ContractsApiResourceCacheKeyFactory>(),
            serviceProvider.GetRequiredService<FormContractDocumentResourceClient>(),
            serviceProvider.GetRequiredService<FieldDefinitionsResourceClient>(),
            serviceProvider.GetRequiredService<VariableDefinitionsResourceClient>(),
            serviceProvider.GetRequiredService<FormVoiceProfileResourceClient>(),
            serviceProvider.GetRequiredService<ContractsApiPreflightFailureMapper>()));

        return services;
    }
}
