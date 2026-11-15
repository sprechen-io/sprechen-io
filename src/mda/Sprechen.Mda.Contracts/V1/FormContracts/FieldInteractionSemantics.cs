namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Leitet die Interaktionskategorie aus Variablentyp und feldbezogenem Code-Stil ab.
/// </summary>
public static class FieldInteractionSemantics
{
    /// <summary>
    /// Ordnet bekannte Basistypen ihren Interaktionskategorien zu. Fehlende oder
    /// unbekannte Typen werden je nach codeStyle als Text oder Code behandelt.
    /// </summary>
    public static string ResolveEffectiveInteractionType(bool codeStyle, string? valueTypeKind)
    {
        var normalizedValueTypeKind = NormalizeValueTypeKind(valueTypeKind);
        return normalizedValueTypeKind switch
        {
            "string" => codeStyle ? EffectiveFieldInteractionTypes.Code : EffectiveFieldInteractionTypes.Text,
            "boolean" => EffectiveFieldInteractionTypes.Boolean,
            "integer" => EffectiveFieldInteractionTypes.Number,
            "enum" => EffectiveFieldInteractionTypes.SingleSelect,
            "set" => EffectiveFieldInteractionTypes.MultiSelect,
            "date" => EffectiveFieldInteractionTypes.Date,
            "time" => EffectiveFieldInteractionTypes.Time,
            _ => codeStyle ? EffectiveFieldInteractionTypes.Code : EffectiveFieldInteractionTypes.Text
        };
    }

    /// <summary>
    /// Prüft, ob die abgeleitete Interaktionskategorie Freitext statt Code ist.
    /// </summary>
    public static bool IsTextField(bool codeStyle, string? valueTypeKind)
        => string.Equals(
            ResolveEffectiveInteractionType(codeStyle, valueTypeKind),
            EffectiveFieldInteractionTypes.Text,
            StringComparison.Ordinal);

    /// <summary>
    /// Erlaubt codeStyle für string sowie für fehlende oder leere Typangaben,
    /// die als string behandelt werden.
    /// </summary>
    public static bool AllowsCodeStyle(string? valueTypeKind)
        => string.Equals(NormalizeValueTypeKind(valueTypeKind), "string", StringComparison.Ordinal);

    private static string NormalizeValueTypeKind(string? valueTypeKind)
        => string.IsNullOrWhiteSpace(valueTypeKind) ? "string" : valueTypeKind.Trim();
}
