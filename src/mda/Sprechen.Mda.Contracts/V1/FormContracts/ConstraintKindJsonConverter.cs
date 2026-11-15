namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Liest bekannte Zeichenketten als <see cref="ConstraintKind"/> und bildet unbekannte
/// oder anders typisierte JSON-Werte auf Unknown ab, damit die Vorprüfung sie melden kann.
/// Beim Schreiben werden nur bekannte Werte aus <see cref="ConstraintKinds"/> zugelassen.
/// </summary>
internal sealed class ConstraintKindJsonConverter : TolerantWireEnumConverter<ConstraintKind>
{
    protected override ConstraintKind Unknown => ConstraintKind.Unknown;

    protected override bool TryToWire(ConstraintKind value, out string wire)
    {
        if (value == ConstraintKind.Unknown)
        {
            wire = string.Empty;
            return false;
        }

        wire = value.ToWireToken();
        return true;
    }

    protected override bool TryFromWire(string? wire, out ConstraintKind value)
        => ConstraintKindExtensions.TryFromWireToken(wire, out value);
}
