namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Ausdrucksarten in Regelbedingungen gemäß expression-language.schema.json.
/// </summary>
public static class ExpressionKinds
{
    public const string And = "and";
    public const string Or = "or";
    public const string Not = "not";
    public const string Equals = "equals";
    public const string NotEquals = "notEquals";
    public const string Compare = "compare";
    public const string In = "in";
    public const string Contains = "contains";
    public const string StartsWith = "startsWith";
    public const string IsSet = "isSet";
    public const string IsNotSet = "isNotSet";
    public const string IsEmpty = "isEmpty";
    public const string IsNotEmpty = "isNotEmpty";
    public const string Custom = "custom";
}
