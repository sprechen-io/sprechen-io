using Microsoft.AspNetCore.Http;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Erzeugt die JSON-Antwort des Versionsendpunkts aus derselben Datenquelle
/// wie die Versionsheader.
/// </summary>
internal static class ApplicationVersionEndpointHandler
{
    public static ApplicationVersionResponse CreateResponse(IApplicationVersionInfoProvider versionInfoProvider)
    {
        ArgumentNullException.ThrowIfNull(versionInfoProvider);

        var current = versionInfoProvider.GetCurrent();
        return new ApplicationVersionResponse(
            current.DisplayVersion,
            current.InformationalVersion,
            current.AssemblyVersion,
            current.SourceRevision,
            current.ShortSourceRevision);
    }

    public static IResult Handle(IApplicationVersionInfoProvider versionInfoProvider)
    {
        return Results.Json(CreateResponse(versionInfoProvider));
    }
}
