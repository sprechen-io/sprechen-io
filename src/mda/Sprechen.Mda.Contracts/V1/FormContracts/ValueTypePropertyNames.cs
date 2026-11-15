namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Eigenschaftsnamen der Werttypvarianten von VariableDefinition.ValueType gemäß
/// variable-definitions.schema.json, einschließlich der Optionseinträge von enum und set.
/// </summary>
public static class ValueTypePropertyNames
{
    public const string Kind = "kind";
    public const string Values = "values";
    public const string Code = "code";
    public const string DisplayName = "displayName";
    public const string FormatHint = "formatHint";
    public const string Min = "min";
    public const string Max = "max";
    public const string MinLength = "minLength";
    public const string MaxLength = "maxLength";
}
