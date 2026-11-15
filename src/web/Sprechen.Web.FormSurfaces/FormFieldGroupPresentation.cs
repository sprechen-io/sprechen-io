using Sprechen.Mda.Contracts.V1.FormSurfaces;
using System.Collections.Immutable;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Bündelt Gruppenbeschreibung, Status und Felder für die Darstellung einer Formulargruppe.
/// </summary>
public sealed record FormFieldGroupPresentation(
    FormSurfaceFieldGroupInfo FieldGroup,
    bool IsEntryFieldGroup,
    FormSurfaceFieldGroupStatus? Status,
    ImmutableArray<FormFieldPresentation> Fields);
