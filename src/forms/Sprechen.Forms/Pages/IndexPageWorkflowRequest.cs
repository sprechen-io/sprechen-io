using Microsoft.AspNetCore.Http;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Bündelt HTTP-Anfrage, Auswahlwerte und Angaben zum Studieneinstieg
/// als Eingabe für den Ladeablauf.
/// </summary>
internal sealed class IndexPageWorkflowRequest
{
    public IndexPageWorkflowRequest(
        HttpRequest? request,
        string? language,
        string? applicationId,
        string? formId,
        string? previousApplicationId,
        IndexPageEntryAdmission? admission = null)
    {
        Request = request;
        Language = language;
        ApplicationId = applicationId;
        FormId = formId;
        PreviousApplicationId = previousApplicationId;
        Admission = admission ?? new IndexPageEntryAdmission();
    }

    public HttpRequest? Request { get; }

    public string? Language { get; }

    public string? ApplicationId { get; }

    public string? FormId { get; }

    public string? PreviousApplicationId { get; }

    /// <summary>
    /// Angaben zum Einstieg über einen Studienlink oder Testlauf,
    /// damit der Ladeablauf die Studienanwendung direkt auswählen kann.
    /// </summary>
    public IndexPageEntryAdmission Admission { get; }
}
