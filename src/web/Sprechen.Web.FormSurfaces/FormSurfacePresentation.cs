using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Bündelt Formular- und Anwendungsdaten, Fokus und Feldgruppen für die Darstellung in Razor.
/// </summary>
public sealed record FormSurfacePresentation(
    FormSurfaceStage Stage,
    string IsoCode,
    string ApplicationId,
    string ApplicationDisplayName,
    string FormId,
    string FormDisplayName,
    string? FocusedFieldId,
    string FocusedFieldLabel,
    ImmutableArray<FormFieldGroupPresentation> FieldGroups);
