using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Erzeugt HTTP-ProblemDetails für Webfehler mit gemeinsamem Fehlercode und Fehlerquelle.
/// </summary>
internal sealed class WebProblemDetailsResultFactory
{
    private readonly ProblemDetailsExtensionFactory _extensionFactory = new();

    public IResult Create(string title, int statusCode, string errorCode, string? detail)
    {
        return TypedResults.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: _extensionFactory.Create("web", errorCode));
    }
}
