using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Erzeugt HTTP-ProblemDetails für Fehler des Vertragsdienstes mit Fehlercode und relativem Anfragepfad.
/// </summary>
internal sealed class CatalogProblemDetailsResultFactory
{
    private readonly ProblemDetailsExtensionFactory _extensionFactory = new();

    public IResult Create(HttpStatusCode responseStatus, string relativePath, string? detail)
    {
        var statusCode = IsHttpStatusCode(responseStatus)
            ? (int)responseStatus
            : StatusCodes.Status502BadGateway;
        var responseDetail = string.IsNullOrWhiteSpace(detail)
            ? $"Catalog service returned {(int)responseStatus} {responseStatus} for '{relativePath}'."
            : detail;

        return TypedResults.Problem(
            title: "Catalog service request failed.",
            detail: responseDetail,
            statusCode: statusCode,
            extensions: _extensionFactory.Create("catalog", $"catalog.{responseStatus}", relativePath));
    }

    private static bool IsHttpStatusCode(HttpStatusCode responseStatus)
    {
        var statusCode = (int)responseStatus;
        return statusCode >= 100 && statusCode < 600;
    }
}
