using Microsoft.AspNetCore.Http;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Fasst HTTP-Anfrage und die Query-Werte lang, app, form und prevApp als Eingabe der Auswahlauflösung zusammen.
/// </summary>
internal sealed class IndexPageSelectionRequest
{
    public IndexPageSelectionRequest(
        HttpRequest? request,
        string? language,
        string? applicationId,
        string? formId,
        string? previousApplicationId)
    {
        Request = request;
        Language = language;
        ApplicationId = applicationId;
        FormId = formId;
        PreviousApplicationId = previousApplicationId;
    }

    public HttpRequest? Request { get; }

    public string? Language { get; }

    public string? ApplicationId { get; }

    public string? FormId { get; }

    public string? PreviousApplicationId { get; }
}
