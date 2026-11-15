using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Bereitet Formularzustände und Vorschauen für die Darstellung in Razor auf.
/// </summary>
public sealed class FormSurfacePresenter : IFormSurfacePresenter
{
    private static readonly Dictionary<string, FormSurfaceFieldValue> EmptyValues = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, FormSurfaceFieldStatus> EmptyFieldStatuses = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, FormSurfaceFieldGroupStatus> EmptyFieldGroupStatuses = new(StringComparer.Ordinal);
    private readonly FormFieldGroupPresentationProjector _fieldGroupPresentationProjector;
    private readonly FormFocusedFieldLabelPresenter _focusedFieldLabelPresenter;

    public FormSurfacePresenter()
        : this(
            new FormFieldGroupPresentationProjector(new FormDisplayValuePresenter()),
            new FormFocusedFieldLabelPresenter())
    {
    }

    internal FormSurfacePresenter(
        FormFieldGroupPresentationProjector fieldGroupPresentationProjector,
        FormFocusedFieldLabelPresenter focusedFieldLabelPresenter)
    {
        _fieldGroupPresentationProjector = fieldGroupPresentationProjector ?? throw new ArgumentNullException(nameof(fieldGroupPresentationProjector));
        _focusedFieldLabelPresenter = focusedFieldLabelPresenter ?? throw new ArgumentNullException(nameof(focusedFieldLabelPresenter));
    }

    public FormSurfacePresentation Present(FormSurfaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new FormSurfacePresentation(
            snapshot.Stage,
            snapshot.IsoCode,
            snapshot.ApplicationId,
            snapshot.ApplicationDisplayName,
            snapshot.FormId == default ? string.Empty : snapshot.FormId.Value,
            snapshot.FormDisplayName,
            snapshot.FocusedFieldId,
            _focusedFieldLabelPresenter.Resolve(snapshot),
            _fieldGroupPresentationProjector.Project(snapshot));
    }

    public FormSurfacePresentation PresentPreview(FormSurfaceDefinition formProjection)
    {
        ArgumentNullException.ThrowIfNull(formProjection);

        return Present(
            new FormSurfaceSnapshot(
                FormSurfaceStage.Active,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                formProjection,
                EmptyValues,
                null,
                EmptyFieldStatuses,
                EmptyFieldGroupStatuses));
    }

}
