using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Leitet Optionen aus einer begrenzten Menge zulässiger Werte ab, damit die Oberfläche diese zur Auswahl anbieten kann.
/// </summary>
internal sealed class FormOneOfOptionProjector
{
    public IReadOnlyList<FormSurfaceOption>? Project(FieldConstraints fieldConstraints)
    {
        ArgumentNullException.ThrowIfNull(fieldConstraints);

        return fieldConstraints.OneOfValues is { Count: > 0 }
            ? fieldConstraints.OneOfValues
                .Select(value => new FormSurfaceOption(value, value))
                .ToArray()
            : null;
    }
}
