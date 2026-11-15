namespace Sprechen.Forms.Pages;

/// <summary>
/// Wandelt die Workflow-Anfrage in die Auswahlanfrage und das Auswahlergebnis in die Ladeanfrage für das gewählte Formular um.
/// </summary>
internal sealed class IndexPageRequestReader
{
    public IndexPageSelectionRequest ReadSelectionRequest(IndexPageWorkflowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new IndexPageSelectionRequest(
            request.Request,
            request.Language,
            request.ApplicationId,
            request.FormId,
            request.PreviousApplicationId);
    }

    public IndexPageSelectedFormRequest ReadSelectedFormRequest(IndexPageSelectionResult selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return new IndexPageSelectedFormRequest(
            selection.SelectedApplication ?? throw new InvalidOperationException("Selected application is required."),
            selection.SelectedForm ?? throw new InvalidOperationException("Selected form is required."),
            selection.SelectedApplicationId,
            selection.SelectedLanguage);
    }
}
