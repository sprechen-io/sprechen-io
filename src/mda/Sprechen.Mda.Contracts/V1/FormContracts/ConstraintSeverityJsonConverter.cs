namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Liest bekannte Zeichenketten als <see cref="ConstraintSeverity"/> und bildet andere
/// JSON-Werte auf Unknown ab, damit die Vorprüfung sie melden kann.
/// Beim Schreiben sind nur bekannte Werte aus <see cref="SeverityLevels"/> erlaubt.
/// </summary>
internal sealed class ConstraintSeverityJsonConverter : TolerantWireEnumConverter<ConstraintSeverity>
{
    protected override ConstraintSeverity Unknown => ConstraintSeverity.Unknown;

    protected override bool TryToWire(ConstraintSeverity value, out string wire)
    {
        if (value == ConstraintSeverity.Unknown)
        {
            wire = string.Empty;
            return false;
        }

        wire = value.ToWireToken();
        return true;
    }

    protected override bool TryFromWire(string? wire, out ConstraintSeverity value)
        => ConstraintSeverityExtensions.TryFromWireToken(wire, out value);
}
