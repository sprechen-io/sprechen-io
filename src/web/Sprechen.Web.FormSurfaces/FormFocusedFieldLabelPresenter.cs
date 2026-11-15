using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Ermittelt die Beschriftung des fokussierten Feldes und verwendet ersatzweise seine Kennung.
/// </summary>
internal sealed class FormFocusedFieldLabelPresenter
{
    public string Resolve(FormSurfaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrWhiteSpace(snapshot.FocusedFieldId))
        {
            return string.Empty;
        }

        var field = snapshot.FormProjection.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.FieldId, snapshot.FocusedFieldId, StringComparison.Ordinal));

        return field?.Label ?? snapshot.FocusedFieldId;
    }
}
