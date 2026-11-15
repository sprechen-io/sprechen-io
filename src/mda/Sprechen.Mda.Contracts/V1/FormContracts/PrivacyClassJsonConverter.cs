namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Liest bekannte Zeichenketten als <see cref="PrivacyClass"/> und bildet andere
/// JSON-Werte auf Unknown ab. Bei nullable-Eigenschaften bleiben fehlende Angaben
/// und JSON-null davon unterscheidbar. Beim Schreiben sind nur bekannte Werte erlaubt.
/// </summary>
internal sealed class PrivacyClassJsonConverter : TolerantWireEnumConverter<PrivacyClass>
{
    protected override PrivacyClass Unknown => PrivacyClass.Unknown;

    protected override bool TryToWire(PrivacyClass value, out string wire)
    {
        if (value == PrivacyClass.Unknown)
        {
            wire = string.Empty;
            return false;
        }

        wire = value.ToWireToken();
        return true;
    }

    protected override bool TryFromWire(string? wire, out PrivacyClass value)
        => PrivacyClassExtensions.TryFromWireToken(wire, out value);
}
