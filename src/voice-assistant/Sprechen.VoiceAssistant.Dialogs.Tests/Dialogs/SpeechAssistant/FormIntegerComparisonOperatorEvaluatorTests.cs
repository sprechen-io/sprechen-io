namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormIntegerComparisonOperatorEvaluatorTests
{
    [Theory]
    [InlineData("eq", 3, 3, true)]
    [InlineData("eq", 3, 4, false)]
    [InlineData("neq", 3, 4, true)]
    [InlineData("neq", 3, 3, false)]
    [InlineData("gt", 5, 4, true)]
    [InlineData("gt", 4, 5, false)]
    [InlineData("gte", 5, 5, true)]
    [InlineData("gte", 4, 5, false)]
    [InlineData("lt", 4, 5, true)]
    [InlineData("lt", 5, 4, false)]
    [InlineData("lte", 5, 5, true)]
    [InlineData("lte", 6, 5, false)]
    public void TryEvaluate_KnownOperator_ReturnsExpectedResult(string operatorValue, int left, int right, bool expected)
    {
        var success = FormIntegerComparisonOperatorEvaluator.TryEvaluate(operatorValue, left, right, out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(" eq ", 3, 3, true)]
    [InlineData(" gt ", 3, 4, false)]
    public void TryEvaluate_TrimmedOperator_ReturnsExpectedResult(string operatorValue, int left, int right, bool expected)
    {
        var success = FormIntegerComparisonOperatorEvaluator.TryEvaluate(operatorValue, left, right, out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryEvaluate_UnknownOperator_ReturnsFalse()
    {
        var success = FormIntegerComparisonOperatorEvaluator.TryEvaluate("between", 1, 2, out var result);

        Assert.False(success);
        Assert.False(result);
    }

    [Fact]
    public void TryEvaluate_NullOperator_ReturnsFalse()
    {
        var success = FormIntegerComparisonOperatorEvaluator.TryEvaluate(null!, 1, 2, out var result);

        Assert.False(success);
        Assert.False(result);
    }

    [Fact]
    public void TryEvaluate_WhitespaceOperator_ReturnsFalse()
    {
        var success = FormIntegerComparisonOperatorEvaluator.TryEvaluate("   ", 1, 2, out var result);

        Assert.False(success);
        Assert.False(result);
    }
}
