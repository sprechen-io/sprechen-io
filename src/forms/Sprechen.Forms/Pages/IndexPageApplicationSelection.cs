using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Bündelt angefragte Anwendungs-Id und gefundenen Deskriptor — null, wenn keine Kataloganwendung zur Id passt.
/// </summary>
internal sealed class IndexPageApplicationSelection
{
    public IndexPageApplicationSelection(string selectedApplicationId, ApplicationDescriptor? selectedApplication)
    {
        SelectedApplicationId = selectedApplicationId;
        SelectedApplication = selectedApplication;
    }

    public string SelectedApplicationId { get; }

    public ApplicationDescriptor? SelectedApplication { get; }
}
