using System.Text.Json;
using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Serialisiert das Navigationsmanifest der gewählten Anwendung
/// für die Einbettung als JSON in die Seite.
/// </summary>
internal sealed class FormsNavigationManifestJsonBuilder
{
    private readonly FormsNavigationManifestProjector _manifestProjector;

    public FormsNavigationManifestJsonBuilder(FormsNavigationManifestProjector manifestProjector)
    {
        _manifestProjector = manifestProjector ?? throw new ArgumentNullException(nameof(manifestProjector));
    }

    public string Build(ApplicationDescriptor selectedApplication, IReadOnlyList<FormOption> forms)
    {
        var navigationManifest = _manifestProjector.Project(selectedApplication, forms);
        return JsonSerializer.Serialize(navigationManifest);
    }
}
