using Sprechen.Contracts.ApiClient.Options;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Forms.Pages;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Web.FormSurfaces;
using Sprechen.Forms.Services;
using Microsoft.Extensions.Options;
using Sprechen.EvaluationTelemetry.Api;
using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.EvaluationTelemetry.JsonlStore;
using Sprechen.ServiceDefaults;
using System.Net;
using System.Net.Http;
using Sprechen.Contracts.ApiClient;
using Sprechen.Forms.Routing;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Forms.Extensions;

/// <summary>
/// Registriert die Dienste und Middleware der Forms-App
/// und bindet die Studienlinks an die Index-Seite an.
/// </summary>
internal static class HostExtensions
{
    public static void AddSprechenForms(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddEnvironmentVariables();
        var hostEndpointPorts = builder.SetupKestrel();
        builder.Services.AddSingleton(hostEndpointPorts);

        builder.AddSprechenServiceDefaults();

        builder.Services.AddOptions<ContractsServiceOptions>()
            .Bind(builder.Configuration.GetSection(ContractsServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddRazorPages();

        builder.Services.AddContractsApiClient("ContractsHttp", (sp, httpClient) =>
        {
            var config = sp.GetRequiredService<IOptions<ContractsServiceOptions>>().Value;
            httpClient.BaseAddress = new Uri(config.BaseAddress);
            httpClient.DefaultRequestVersion = HttpVersion.Version20;
            // Die Katalogabfrage muss auch HTTP/1.1-Endpunkte ohne TLS im Cluster erreichen können.
            httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        });

        builder.Services.AddScoped<IFormsCatalogDataClient, FormsCatalogDataClient>();
        builder.Services.AddScoped<IMdaFormContractDataClient, MdaFormContractDataClient>();
        builder.Services.AddScoped<FormsCatalogLoader>();
        builder.Services.AddScoped<IndexPageFormsEndpoint>();
        builder.Services.AddScoped(serviceProvider => new IndexPageRuntime(
            serviceProvider.GetRequiredService<IndexPageWorkflow>(),
            serviceProvider.GetRequiredService<IndexPageFormsEndpoint>()));
        builder.Services.AddScoped<IndexPageSelectedFormLoader>();
        builder.Services.AddScoped<IndexPageWorkflow>();
        builder.Services.AddSingleton<FormsNavigationManifestJsonBuilder>();
        builder.Services.AddSingleton<FormsNavigationManifestProjector>();
        builder.Services.AddSingleton<FormsPageAcceptedLanguageHeaderResolver>();
        builder.Services.AddSingleton<FormsPageBrowserLanguageResolver>();
        builder.Services.AddSingleton<FormsPageCatalogLanguageFallbackResolver>();
        builder.Services.AddSingleton<FormsPageLanguageAvailabilityResolver>();
        builder.Services.AddSingleton<FormsPageLanguageCodeNormalizer>();
        builder.Services.AddSingleton<FormsPageLanguageResolver>();
        builder.Services.AddSingleton<IndexPageApplicationOptionsResolver>();
        builder.Services.AddSingleton<IndexPageFormContractTextResolver>();
        builder.Services.AddSingleton<IndexPageFormOptionResolver>();
        builder.Services.AddSingleton<IndexPageFormsEndpointProjection>();
        builder.Services.AddSingleton<IndexPageRequestReader>();
        builder.Services.AddSingleton<IndexPageSelectedApplicationResolver>();
        builder.Services.AddSingleton<IndexPageSelectedFormLoadErrorMapper>();
        builder.Services.AddSingleton<IndexPageSelectedFormProjectionBuilder>();
        builder.Services.AddSingleton<IndexPageSelectedFormResolver>();
        builder.Services.AddSingleton<IndexPageSelectionResolver>();
        builder.Services.AddSingleton<UmfrageStudyLinkRewriter>();
        builder.Services.AddSingleton<IFormSurfaceDefinitionBuilder, FormSurfaceDefinitionBuilder>();
        builder.Services.AddSingleton<IFormSurfacePresenter, FormSurfacePresenter>();
        builder.Services.AddEvaluationTelemetryIngestion(StudyCondition.A);
        builder.Services.AddEvaluationTelemetryJsonlStore(builder.Configuration);
        builder.Services.AddEvaluationTelemetryApi(builder.Configuration);
    }

    public static void UseSprechenForms(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var hostEndpointPorts = app.Services.GetRequiredService<FormsHostEndpointPorts>();
        app.UseSprechenHealthEndpointIsolation(
            hostEndpointPorts.ApplicationPort,
            hostEndpointPorts.HealthPort);
        app.UseSprechenHttpsRedirection();

        // Studienlink: intern die Index-Seite unter /umfrage/<token> ausliefern
        // Die Browser-URL bleibt erhalten, damit das Client-Skript das Token lesen kann.
        // Auch fehlerhafte Tokens oder zusätzliche Pfadsegmente werden zur Index-Seite geleitet.
        // StudyLinkIdentity kennzeichnet ihre Ablehnung. Die Umschreibung muss vor dem Routing erfolgen.
        app.Use(async (context, next) =>
        {
            var rewriter = context.RequestServices.GetRequiredService<UmfrageStudyLinkRewriter>();
            if (rewriter.TryRewriteToIndex(context.Request.Path, out var rewritten, out var identity))
            {
                context.Request.Path = rewritten;
                if (identity is not null)
                {
                    context.Features.Set(identity);
                }
            }

            await next();
        });

        app.UseStaticFiles();
        app.UseRouting();
        app.MapSprechenDefaultEndpoints();
        app.MapRazorPages();

        app.UseEvaluationTelemetryApi();
    }
}
