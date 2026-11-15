namespace Sprechen.Forms.ViewComponents;

/// <summary>
/// Erzeugt Links zur Formularauswahl mit Anwendung, Formular und Sprache,
/// ohne Teilnehmerkennung oder andere Studienparameter in der Query-Zeichenfolge.
/// </summary>
internal sealed class FormNavigationUrlBuilder
{
    public string Build(FormNavigationModel model, string formId)
    {
        ArgumentNullException.ThrowIfNull(model);


        // Die Studienoberfläche behandelt ihre Kennung über den /umfrage/-Pfad.
        var queryParameters = RequiredParameters(model, formId)
            .Select(parameter => FormatParameter(parameter.Key, parameter.Value));

        return "/?" + string.Join("&", queryParameters);
    }

    private IReadOnlyList<KeyValuePair<string, string>> RequiredParameters(FormNavigationModel model, string formId)
    {
        return
        [
            new KeyValuePair<string, string>("app", model.ApplicationId),
            new KeyValuePair<string, string>("form", formId),
            new KeyValuePair<string, string>("lang", model.Language)
        ];
    }

    private string FormatParameter(string key, string value)
    {
        return $"{key}={Uri.EscapeDataString(value)}";
    }
}
