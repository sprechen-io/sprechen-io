using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Stellt die Formularprojektion unabhängig von Razor bereit, auch für Vorschauen und Tests.
/// </summary>
public interface IFormSurfacePresenter
{
    FormSurfacePresentation Present(FormSurfaceSnapshot snapshot);

    FormSurfacePresentation PresentPreview(FormSurfaceDefinition formProjection);
}
