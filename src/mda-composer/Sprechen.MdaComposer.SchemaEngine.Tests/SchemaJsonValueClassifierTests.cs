using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaJsonValueClassifierTests
{
    private readonly SchemaJsonValueClassifier _classifier = new();

    [Theory]
    [InlineData(null, "null")]
    [InlineData("text", "String")]
    [InlineData(42, "Ganzzahl")]
    [InlineData(42.5, "Zahl")]
    [InlineData(true, "Boolean")]
    public void GetDisplayName_UsesCanonicalRuntimeNodeNames(object? value, string expectedName)
    {
        var node = value switch
        {
            null => null,
            string stringValue => JsonValue.Create(stringValue),
            int integerValue => JsonValue.Create(integerValue),
            double numberValue => JsonValue.Create(numberValue),
            bool booleanValue => JsonValue.Create(booleanValue),
            _ => throw new InvalidOperationException("Unsupported test value."),
        };

        var displayName = _classifier.GetDisplayName(node);

        Assert.Equal(expectedName, displayName);
    }

    [Fact]
    public void IntegerCompatibility_AcceptsWholeDoubleValues()
    {
        var node = JsonValue.Create(42.0)!;

        Assert.True(_classifier.IsIntegerValue(node));
        Assert.True(_classifier.IsNumberValue(node));
    }
}
