namespace Sprechen.Web.HostSupport.Options;

/// <summary>
/// Konfiguriert den Titel der OpenAPI-Oberfläche und ihre Freigabe außerhalb der Entwicklungsumgebung.
/// </summary>
internal sealed class OpenApiUiOptions
{
    public const string SectionName = "OpenApiUi";

    public bool Enabled { get; set; }

    public string Title { get; set; } = string.Empty;
}
