using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Sprechen.Contracts.ApiClient;
using Sprechen.Contracts.ApiClient.Options;
using Sprechen.EvaluationTelemetry.Api;
using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.EvaluationTelemetry.JsonlStore;
using Sprechen.ServiceDefaults;
using Sprechen.Web.HostSupport.Endpoints;
using Sprechen.Web.HostSupport.Endpoints.Adapter;
using Sprechen.Web.HostSupport.Options;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Bündelt Dienstregistrierung und HTTP-Konfiguration der gehosteten Voice-Anwendung.
/// </summary>
public static class HostedVoiceWebApplicationExtensions
{
    private const int ProductionApplicationPort = 8080;
    private const int ProductionHealthPort = 8081;
    private const int DevelopmentApplicationPort = 5087;
    private const int DevelopmentHealthPort = 5088;
    private const int HttpsDevelopmentPort = 7249;

    public static void AddHostedVoiceWeb(this WebApplicationBuilder builder, string openApiTitle)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(openApiTitle);

        var (applicationPort, healthPort) = ResolveEndpointPorts(builder.Environment);
        builder.ConfigureSprechenKestrelEndpoints(
            applicationPort,
            applicationProtocols: HttpProtocols.Http1AndHttp2,
            healthPort,
            httpsDevelopmentPort: HttpsDevelopmentPort);
        builder.AddSprechenServiceDefaults();

        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        builder.Services.AddOpenApi(options =>
        {
            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = openApiTitle;
                document.Info.Version = "v1";
                return Task.CompletedTask;
            });
        });

        builder.Services.AddOptions<SpeechServiceOptions>()
            .Bind(builder.Configuration.GetSection(SpeechServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<ContractsServiceOptions>()
            .Bind(builder.Configuration.GetSection(ContractsServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddContractsApiClient("ContractsHttp", (serviceProvider, httpClient) =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<ContractsServiceOptions>>().Value;
            httpClient.BaseAddress = new Uri(config.BaseAddress);
            httpClient.DefaultRequestVersion = HttpVersion.Version20;
            // Die Vertragsentdeckung kann Klartext-Endpunkte im Cluster nutzen, die nur HTTP/1.1 aushandeln.
            httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        });

        builder.Services.AddOptions<ServiceAuthOptions>()
            .Bind(builder.Configuration.GetSection(ServiceAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<OpenApiUiOptions>()
            .Bind(builder.Configuration.GetSection(OpenApiUiOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.Title))
                {
                    options.Title = openApiTitle;
                }
            });

        builder.Services.AddHostedVoiceGrpc();
        builder.Services.AddEvaluationTelemetryIngestion(StudyCondition.B);
        builder.Services.AddEvaluationTelemetryJsonlStore(builder.Configuration);
        builder.Services.AddEvaluationTelemetryApi(builder.Configuration);
    }

    public static void UseHostedVoiceWeb<TRootComponent, TAdditionalAssemblyMarker>(this WebApplication app)
        where TRootComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(app);

        var (applicationPort, healthPort) = ResolveEndpointPorts(app.Environment);
        app.UseSprechenHealthEndpointIsolation(applicationPort, healthPort);
        app.UseSprechenHttpsRedirection();
        app.UseNoIndexHeaders();
        app.UseNoCacheForHtmlResponses();
        app.UseSpeechSecurityHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        var openApiUiOptions = app.Services.GetRequiredService<IOptions<OpenApiUiOptions>>().Value;
        if (app.Environment.IsDevelopment() || openApiUiOptions.Enabled)
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/scalar/v1", options =>
            {
                options
                    .WithTitle(openApiUiOptions.Title)
                    .WithOpenApiRoutePattern("/openapi/{documentName}.json");
            });
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();
        app.MapSprechenDefaultEndpoints();
        app.MapMdaContractsProxy();
        app.MapStaticAssets();

        app.MapRazorComponents<TRootComponent>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(TAdditionalAssemblyMarker).Assembly);

        app.UseHostedVoiceGrpc();

        app.UseEvaluationTelemetryApi();
        app.MapPlaybackDiagnosticsEndpoint();
        app.MapCaptureDiagnosticsEndpoint();
        app.MapWakeDiagnosticsEndpoint();
        app.MapDialogDecisionDiagnosticsEndpoint();
    }

    private static (int ApplicationPort, int HealthPort) ResolveEndpointPorts(IHostEnvironment environment)
        => environment.IsDevelopment()
            ? (DevelopmentApplicationPort, DevelopmentHealthPort)
            : (ProductionApplicationPort, ProductionHealthPort);
}
