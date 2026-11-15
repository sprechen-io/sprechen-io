using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Registriert den Versionsendpunkt, damit Clients den laufenden
/// Anwendungsstand über eine GET-Anfrage abrufen können.
/// </summary>
public static class ApplicationVersionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSprechenApplicationVersionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/version", ApplicationVersionEndpointHandler.Handle)
            .WithName("GetApplicationVersion")
            .Produces<ApplicationVersionResponse>(StatusCodes.Status200OK);

        return endpoints;
    }
}
