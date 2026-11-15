namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Arten von Wertausdrücken gemäß expression-language.schema.json.
/// </summary>
public static class ValueExpressionKinds
{
    /// <summary>
    /// var: Referenz auf eine Variable.
    /// </summary>
    public const string Var = "var";

    /// <summary>
    /// const: Konstanter Literalwert.
    /// </summary>
    public const string Const = "const";
}
