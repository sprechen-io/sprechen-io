namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Liest bekannte Zeichenketten als <see cref="ConstraintScope"/> und bildet andere
/// vorhandene Werte auf Unknown ab, damit die Vorprüfung sie melden kann.
/// Beim Schreiben sind nur bekannte Übertragungswerte erlaubt. Bei einer nullable
/// Scope-Eigenschaft bleiben fehlende Angaben und JSON-null vom Unknown-Wert unterscheidbar.
/// </summary>
internal sealed class ConstraintScopeJsonConverter : TolerantWireEnumConverter<ConstraintScope>
{
    protected override ConstraintScope Unknown => ConstraintScope.Unknown;

    protected override bool TryToWire(ConstraintScope value, out string wire)
    {
        if (value == ConstraintScope.Unknown)
        {
            wire = string.Empty;
            return false;
        }

        wire = value.ToWireToken();
        return true;
    }

    protected override bool TryFromWire(string? wire, out ConstraintScope value)
        => ConstraintScopeExtensions.TryFromWireToken(wire, out value);
}
