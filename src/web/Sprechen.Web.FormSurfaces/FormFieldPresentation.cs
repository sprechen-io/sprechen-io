using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Bündelt Felddefinition, Anzeigewert, Fokus und Status für die Darstellung.
/// </summary>
public sealed record FormFieldPresentation(
    FormSurfaceField Field,
    string? DisplayValue,
    bool HasDisplayValue,
    bool IsFocused,
    FormSurfaceFieldStatus? Status);
