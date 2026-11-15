using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Leitet eine Formularoberfläche aus dem Vertrag ab, sodass HTML- und
/// Sprachoberfläche denselben Aufbau verwenden können.
/// </summary>
public interface IFormSurfaceDefinitionBuilder
{
    FormSurfaceDefinition BuildFormSurfaceDefinition(FormContract contract, string requestedLanguage, PolitenessLevel politenessLevel);
}
