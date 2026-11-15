using System.Text.Json;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormRuleEngineTests
{
    #region Helper Methods

    private static JsonElement ParseJson(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    private static FormRuleEngine CreateEngine(Dictionary<string, FormValue>? values = null)
    {
        values ??= [];
        return new FormRuleEngine(variableId =>
            values.TryGetValue(variableId, out var value) ? value : null);
    }

    #endregion

    #region And Expression

    [Fact]
    public void EvaluateExpression_And_AllTrue_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["a"] = FormValue.FromBoolean(true),
            ["b"] = FormValue.FromBoolean(true)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "and",
                "expressions": [
                    { "kind": "isSet", "variableId": "a" },
                    { "kind": "isSet", "variableId": "b" }
                ]
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_And_OneFalse_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["a"] = FormValue.FromBoolean(true)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "and",
                "expressions": [
                    { "kind": "isSet", "variableId": "a" },
                    { "kind": "isSet", "variableId": "b" }
                ]
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_And_EmptyArray_ReturnsTrue()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "and", "expressions": [] }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_And_MissingExpressions_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "and" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region Or Expression

    [Fact]
    public void EvaluateExpression_Or_OneTrue_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["a"] = FormValue.FromBoolean(true)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "or",
                "expressions": [
                    { "kind": "isSet", "variableId": "a" },
                    { "kind": "isSet", "variableId": "b" }
                ]
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Or_AllFalse_ReturnsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""
            {
                "kind": "or",
                "expressions": [
                    { "kind": "isSet", "variableId": "a" },
                    { "kind": "isSet", "variableId": "b" }
                ]
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Or_EmptyArray_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "or", "expressions": [] }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region Not Expression

    [Fact]
    public void EvaluateExpression_Not_InvertsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["a"] = FormValue.FromBoolean(true)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "not",
                "expression": { "kind": "isSet", "variableId": "a" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Not_InvertsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""
            {
                "kind": "not",
                "expression": { "kind": "isSet", "variableId": "a" }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Not_MissingExpression_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "not" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region Equals Expression

    [Fact]
    public void EvaluateExpression_Equals_SameString_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "equals",
                "left": { "kind": "var", "variableId": "name" },
                "right": { "kind": "const", "value": "Max" }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Equals_DifferentString_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "equals",
                "left": { "kind": "var", "variableId": "name" },
                "right": { "kind": "const", "value": "Anna" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Equals_SameInteger_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["age"] = FormValue.FromInteger(42)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "equals",
                "left": { "kind": "var", "variableId": "age" },
                "right": { "kind": "const", "value": 42 }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Equals_SameBoolean_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["active"] = FormValue.FromBoolean(true)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "equals",
                "left": { "kind": "var", "variableId": "active" },
                "right": { "kind": "const", "value": true }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Equals_TwoVariables_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["a"] = FormValue.FromString("same"),
            ["b"] = FormValue.FromString("same")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "equals",
                "left": { "kind": "var", "variableId": "a" },
                "right": { "kind": "var", "variableId": "b" }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Equals_MissingVariable_ReturnsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""
            {
                "kind": "equals",
                "left": { "kind": "var", "variableId": "missing" },
                "right": { "kind": "const", "value": "test" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Equals_MissingLeft_ReturnsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""
            {
                "kind": "equals",
                "right": { "kind": "const", "value": "test" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region NotEquals Expression

    [Fact]
    public void EvaluateExpression_NotEquals_SameValues_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "notEquals",
                "left": { "kind": "var", "variableId": "name" },
                "right": { "kind": "const", "value": "Max" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_NotEquals_DifferentValues_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "notEquals",
                "left": { "kind": "var", "variableId": "name" },
                "right": { "kind": "const", "value": "Anna" }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_NotEquals_MissingVariable_ReturnsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""
            {
                "kind": "notEquals",
                "left": { "kind": "var", "variableId": "missing" },
                "right": { "kind": "const", "value": "Anna" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region Compare Expression

    [Fact]
    public void EvaluateExpression_Compare_GreaterThan_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["age"] = FormValue.FromInteger(42)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "compare",
                "operator": "gt",
                "left": { "kind": "var", "variableId": "age" },
                "right": { "kind": "const", "value": 18 }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Compare_LessThan_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["age"] = FormValue.FromInteger(42)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "compare",
                "operator": "lt",
                "left": { "kind": "var", "variableId": "age" },
                "right": { "kind": "const", "value": 18 }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Compare_EqualWithGte_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["age"] = FormValue.FromInteger(42)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "compare",
                "operator": "gte",
                "left": { "kind": "var", "variableId": "age" },
                "right": { "kind": "const", "value": 42 }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Compare_NonIntegerOperand_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "compare",
                "operator": "eq",
                "left": { "kind": "var", "variableId": "name" },
                "right": { "kind": "const", "value": "Max" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region In Expression

    [Fact]
    public void EvaluateExpression_In_ValueInSet_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["country"] = FormValue.FromString("DE")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "in",
                "value": { "kind": "var", "variableId": "country" },
                "set": [
                    { "kind": "const", "value": "AT" },
                    { "kind": "const", "value": "DE" },
                    { "kind": "const", "value": "CH" }
                ]
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_In_ValueNotInSet_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["country"] = FormValue.FromString("FR")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "in",
                "value": { "kind": "var", "variableId": "country" },
                "set": [
                    { "kind": "const", "value": "AT" },
                    { "kind": "const", "value": "DE" },
                    { "kind": "const", "value": "CH" }
                ]
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_In_EmptySet_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["country"] = FormValue.FromString("DE")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "in",
                "value": { "kind": "var", "variableId": "country" },
                "set": []
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region Contains Expression

    [Fact]
    public void EvaluateExpression_Contains_SubstringPresent_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["text"] = FormValue.FromString("Max Mustermann")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "contains",
                "text": { "kind": "var", "variableId": "text" },
                "substring": { "kind": "const", "value": "must" }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Contains_SubstringAbsent_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["text"] = FormValue.FromString("Max Mustermann")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "contains",
                "text": { "kind": "var", "variableId": "text" },
                "substring": { "kind": "const", "value": "anna" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_Contains_EmptySubstring_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["text"] = FormValue.FromString("Max Mustermann")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "contains",
                "text": { "kind": "var", "variableId": "text" },
                "substring": { "kind": "const", "value": "" }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    #endregion

    #region StartsWith Expression

    [Fact]
    public void EvaluateExpression_StartsWith_PrefixMatch_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["text"] = FormValue.FromString("Max Mustermann")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "startsWith",
                "text": { "kind": "var", "variableId": "text" },
                "prefix": { "kind": "const", "value": "max" }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_StartsWith_NoMatch_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["text"] = FormValue.FromString("Max Mustermann")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""
            {
                "kind": "startsWith",
                "text": { "kind": "var", "variableId": "text" },
                "prefix": { "kind": "const", "value": "anna" }
            }
            """);

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region IsSet Expression

    [Fact]
    public void EvaluateExpression_IsSet_VariableExists_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""{ "kind": "isSet", "variableId": "name" }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsSet_VariableMissing_ReturnsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "kind": "isSet", "variableId": "name" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsSet_MissingVariableId_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "isSet" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsSet_EmptyVariableId_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "isSet", "variableId": "" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region IsNotSet Expression

    [Fact]
    public void EvaluateExpression_IsNotSet_VariableMissing_ReturnsTrue()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "kind": "isNotSet", "variableId": "name" }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsNotSet_VariableExists_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""{ "kind": "isNotSet", "variableId": "name" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsNotSet_EmptyVariableId_ReturnsTrue()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "isNotSet", "variableId": "" }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    #endregion

    #region IsEmpty Expression

    [Fact]
    public void EvaluateExpression_IsEmpty_MissingVariable_ReturnsTrue()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "isEmpty", "variableId": "name" }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsEmpty_EmptyString_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString(string.Empty)
        };
        var engine = CreateEngine(values);
        var expression = ParseJson("""{ "kind": "isEmpty", "variableId": "name" }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsEmpty_NonEmptyValue_ReturnsFalse()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);
        var expression = ParseJson("""{ "kind": "isEmpty", "variableId": "name" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region IsNotEmpty Expression

    [Fact]
    public void EvaluateExpression_IsNotEmpty_NonEmptyValue_ReturnsTrue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);
        var expression = ParseJson("""{ "kind": "isNotEmpty", "variableId": "name" }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_IsNotEmpty_MissingVariable_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "isNotEmpty", "variableId": "name" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    #endregion

    #region Nested Expressions

    [Fact]
    public void EvaluateExpression_NestedAndOr_EvaluatesCorrectly()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["a"] = FormValue.FromBoolean(true),
            ["c"] = FormValue.FromBoolean(true)
        };
        var engine = CreateEngine(values);

        // (isSet(a) UND isSet(b)) ODER isSet(c) ergibt wahr, weil c gesetzt ist.
        var expression = ParseJson("""
            {
                "kind": "or",
                "expressions": [
                    {
                        "kind": "and",
                        "expressions": [
                            { "kind": "isSet", "variableId": "a" },
                            { "kind": "isSet", "variableId": "b" }
                        ]
                    },
                    { "kind": "isSet", "variableId": "c" }
                ]
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_NotWithEquals_EvaluatesCorrectly()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["status"] = FormValue.FromString("active")
        };
        var engine = CreateEngine(values);

        // NOT (status == "inactive")
        var expression = ParseJson("""
            {
                "kind": "not",
                "expression": {
                    "kind": "equals",
                    "left": { "kind": "var", "variableId": "status" },
                    "right": { "kind": "const", "value": "inactive" }
                }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_DeeplyNested_EvaluatesCorrectly()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["x"] = FormValue.FromInteger(5)
        };
        var engine = CreateEngine(values);

        // NOT (NOT (x == 5)) = NOT (NOT true) = NOT false = true
        var expression = ParseJson("""
            {
                "kind": "not",
                "expression": {
                    "kind": "not",
                    "expression": {
                        "kind": "equals",
                        "left": { "kind": "var", "variableId": "x" },
                        "right": { "kind": "const", "value": 5 }
                    }
                }
            }
            """);

        Assert.True(engine.EvaluateExpression(expression));
    }

    #endregion

    #region TryEvaluateValueExpression

    [Fact]
    public void TryEvaluateValueExpression_Var_ReturnsValue()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["name"] = FormValue.FromString("Max")
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""{ "kind": "var", "variableId": "name" }""");

        var result = engine.TryEvaluateValueExpression(expression, out var value);

        Assert.True(result);
        Assert.NotNull(value);
        Assert.Equal("Max", value.Value.String);
    }

    [Fact]
    public void TryEvaluateValueExpression_Var_MissingVariable_ReturnsNullValue()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "kind": "var", "variableId": "missing" }""");

        var result = engine.TryEvaluateValueExpression(expression, out var value);

        Assert.True(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryEvaluateValueExpression_ConstString_ReturnsValue()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "kind": "const", "value": "hello" }""");

        var result = engine.TryEvaluateValueExpression(expression, out var value);

        Assert.True(result);
        Assert.NotNull(value);
        Assert.Equal("hello", value.Value.String);
    }

    [Fact]
    public void TryEvaluateValueExpression_ConstInteger_ReturnsValue()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "kind": "const", "value": 42 }""");

        var result = engine.TryEvaluateValueExpression(expression, out var value);

        Assert.True(result);
        Assert.NotNull(value);
        Assert.Equal(42, value.Value.Integer);
    }

    [Fact]
    public void TryEvaluateValueExpression_ConstBoolean_ReturnsValue()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "kind": "const", "value": true }""");

        var result = engine.TryEvaluateValueExpression(expression, out var value);

        Assert.True(result);
        Assert.NotNull(value);
        Assert.True(value.Value.Boolean);
    }

    [Fact]
    public void TryEvaluateValueExpression_UnknownKind_ReturnsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "kind": "unknown", "value": "test" }""");

        var result = engine.TryEvaluateValueExpression(expression, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryEvaluateValueExpression_MissingKind_ReturnsFalse()
    {
        var engine = CreateEngine();

        var expression = ParseJson("""{ "value": "test" }""");

        var result = engine.TryEvaluateValueExpression(expression, out _);

        Assert.False(result);
    }

    #endregion

    #region TryParseConstValue (Static)

    [Fact]
    public void TryParseConstValue_True_ReturnsBooleanTrue()
    {
        var json = ParseJson("true");

        var result = FormRuleEngine.TryParseConstValue(json, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.Boolean, value.Kind);
        Assert.True(value.Boolean);
    }

    [Fact]
    public void TryParseConstValue_False_ReturnsBooleanFalse()
    {
        var json = ParseJson("false");

        var result = FormRuleEngine.TryParseConstValue(json, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.Boolean, value.Kind);
        Assert.False(value.Boolean);
    }

    [Fact]
    public void TryParseConstValue_Integer_ReturnsInteger()
    {
        var json = ParseJson("123");

        var result = FormRuleEngine.TryParseConstValue(json, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.Integer, value.Kind);
        Assert.Equal(123, value.Integer);
    }

    [Fact]
    public void TryParseConstValue_NegativeInteger_ReturnsInteger()
    {
        var json = ParseJson("-42");

        var result = FormRuleEngine.TryParseConstValue(json, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.Integer, value.Kind);
        Assert.Equal(-42, value.Integer);
    }

    [Fact]
    public void TryParseConstValue_String_ReturnsString()
    {
        var json = ParseJson("\"hello world\"");

        var result = FormRuleEngine.TryParseConstValue(json, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.String, value.Kind);
        Assert.Equal("hello world", value.String);
    }

    [Fact]
    public void TryParseConstValue_Null_ReturnsFalse()
    {
        var json = ParseJson("null");

        var result = FormRuleEngine.TryParseConstValue(json, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseConstValue_Array_ReturnsFalse()
    {
        var json = ParseJson("[1, 2, 3]");

        var result = FormRuleEngine.TryParseConstValue(json, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseConstValue_Object_ReturnsFalse()
    {
        var json = ParseJson("""{ "key": "value" }""");

        var result = FormRuleEngine.TryParseConstValue(json, out _);

        Assert.False(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EvaluateExpression_NotAnObject_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("\"not an object\"");

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_MissingKind_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "variableId": "x" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_UnknownKind_ThrowsInvalidOperationException()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "unknownKind" }""");

        Assert.Throws<InvalidOperationException>(() => engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_CustomKind_ThrowsInvalidOperationException()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""
            {
                "kind": "custom",
                "customName": "myExpression",
                "payload": {}
            }
            """);

        Assert.Throws<InvalidOperationException>(() => engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_EmptyKind_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "" }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_WhitespaceKind_ReturnsFalse()
    {
        var engine = CreateEngine();
        var expression = ParseJson("""{ "kind": "   " }""");

        Assert.False(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void EvaluateExpression_KindIsCaseInsensitive()
    {
        var values = new Dictionary<string, FormValue>
        {
            ["x"] = FormValue.FromBoolean(true)
        };
        var engine = CreateEngine(values);

        var expression = ParseJson("""{ "kind": "ISSET", "variableId": "x" }""");

        Assert.True(engine.EvaluateExpression(expression));
    }

    [Fact]
    public void Constructor_ThrowsOnNullGetVariableValue()
    {
        Assert.Throws<ArgumentNullException>(() => new FormRuleEngine(null!));
    }

    #endregion
}

