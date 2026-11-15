namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Liest bekannte Zeichenketten als <see cref="FieldEditability"/> und bildet andere
/// JSON-Werte auf Unknown ab. Beim Schreiben werden Unknown und nicht definierte
/// Enumwerte abgelehnt, weil sie keine gültige Übertragungsdarstellung besitzen.
/// </summary>
internal sealed class FieldEditabilityJsonConverter : TolerantWireEnumConverter<FieldEditability>
{
    protected override FieldEditability Unknown => FieldEditability.Unknown;

    protected override bool TryToWire(FieldEditability value, out string wire)
    {
        if (value == FieldEditability.Unknown)
        {
            wire = string.Empty;
            return false;
        }

        wire = value.ToWireToken();
        return true;
    }

    protected override bool TryFromWire(string? wire, out FieldEditability value)
        => FieldEditabilityExtensions.TryFromWireToken(wire, out value);
}
