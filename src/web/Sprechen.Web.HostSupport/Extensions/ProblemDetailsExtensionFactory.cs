namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Erzeugt strukturierte ProblemDetails-Erweiterungen für Fehlerquelle, Fehlercode und optionalen Anfragepfad.
/// </summary>
internal sealed class ProblemDetailsExtensionFactory
{
    public IDictionary<string, object?> Create(
        string errorSource,
        string errorCode,
        string? relativePath = null)
    {
        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [ProblemDetailsExtensionNames.ErrorSource] = errorSource,
            [ProblemDetailsExtensionNames.ErrorCode] = errorCode,
        };

        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            extensions["relativePath"] = relativePath;
        }

        return extensions;
    }
}
