namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Arten von Validierungsregeln gemäß constraint-language.schema.json.
/// </summary>
public static class ConstraintKinds
{
    public const string Required = "required";
    public const string Pattern = "pattern";
    public const string MinLength = "minLength";
    public const string MaxLength = "maxLength";
    public const string Min = "min";
    public const string Max = "max";
    public const string Range = "range";
    public const string DateMin = "dateMin";
    public const string DateMax = "dateMax";
    public const string DateRange = "dateRange";
    public const string Equals = "equals";
    public const string NotEquals = "notEquals";
    public const string OneOf = "oneOf";
    public const string AllOrNone = "allOrNone";
    public const string Compare = "compare";
    public const string CompareVariables = "compareVariables";
    public const string CustomConstraint = "customConstraint";
}
