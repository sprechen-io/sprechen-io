namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Parameternamen der einzelnen Varianten von ConstraintDefinition gemäß
/// constraint-language.schema.json. Die gemeinsamen Basisfelder aus ConstraintBase
/// sind nicht enthalten.
/// </summary>
public static class ConstraintPropertyNames
{
    public const string Pattern = "pattern";
    public const string MinLength = "minLength";
    public const string MaxLength = "maxLength";
    public const string Min = "min";
    public const string Max = "max";
    public const string MinDate = "minDate";
    public const string MaxDate = "maxDate";
    public const string Value = "value";
    public const string Values = "values";
    public const string VariableIds = "variableIds";
    public const string Operator = "operator";
    public const string LeftVariableId = "leftVariableId";
    public const string RightVariableId = "rightVariableId";
    public const string CustomConstraintName = "customConstraintName";
    public const string Payload = "payload";
}
