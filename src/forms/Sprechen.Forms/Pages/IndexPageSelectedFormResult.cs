using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Web.FormSurfaces;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Trägt das Ergebnis des Formularladens an die Seite: die Anzeigedaten oder, beim Fehlschlag, nur den Fehlercode.
/// </summary>
internal sealed class IndexPageSelectedFormResult
{
    public string EntryFieldGroupId { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, FormSurfaceFieldGroupInfo> FieldGroupsById { get; init; } =
        new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<FormSurfaceField> Fields { get; init; } = Array.Empty<FormSurfaceField>();

    public FormSurfacePresentation? Surface { get; init; }

    public string FormContractJson { get; init; } = string.Empty;

    public string FormDisplayName { get; init; } = string.Empty;

    public string FormDescription { get; init; } = string.Empty;

    public string HtmlProjectionProfile { get; init; } = HtmlProjectionProfiles.A11yFirst;

    public string? ErrorCode { get; init; }
}
