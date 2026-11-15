using Microsoft.AspNetCore.Mvc;

namespace Sprechen.Forms.ViewComponents;

/// <summary>
/// Bündelt Zurück-, Weiter-, Absenden- und Abbrechen-Steuerung als Ansichtskomponente,
/// damit die Formularnavigation auch mehrere Formulare des Katalogs verbindet.
/// </summary>
public sealed class FormNavigationViewComponent : ViewComponent
{

    public IViewComponentResult Invoke(FormNavigationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return View(model);
    }
}

