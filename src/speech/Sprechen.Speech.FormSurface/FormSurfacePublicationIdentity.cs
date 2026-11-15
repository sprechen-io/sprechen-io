namespace Sprechen.Speech.FormSurface;

/// <summary>
/// Hält Sprache und Formularzuordnung für die Zusammenstellung eines Beobachtersnapshots,
/// damit die Schreibseite ihre Metadaten getrennt vom veröffentlichten Snapshot übergeben kann.
/// Der Beobachtervertrag bleibt <c>FormSurfaceSnapshot</c>. Diese Identität dient ausschließlich der Zusammenstellung.
/// </summary>
public sealed class FormSurfacePublicationIdentity
{
    public FormSurfacePublicationIdentity(
        string isoCode,
        string applicationId,
        string applicationDisplayName,
        string formId,
        string formDisplayName)
    {
        ArgumentNullException.ThrowIfNull(isoCode);
        ArgumentNullException.ThrowIfNull(applicationId);
        ArgumentNullException.ThrowIfNull(applicationDisplayName);
        ArgumentNullException.ThrowIfNull(formId);
        ArgumentNullException.ThrowIfNull(formDisplayName);

        IsoCode = isoCode;
        ApplicationId = applicationId;
        ApplicationDisplayName = applicationDisplayName;
        FormId = formId;
        FormDisplayName = formDisplayName;
    }

    public string IsoCode { get; }

    public string ApplicationId { get; }

    public string ApplicationDisplayName { get; }

    public string FormId { get; }

    public string FormDisplayName { get; }
}
