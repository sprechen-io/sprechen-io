using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSetValueConstraintEvaluatorTests
{
    [Fact]
    public void IsSatisfied_WhenMinConstraintMatchesIntegerValue_ReturnsTrue()
    {
        var evaluator = new FormSetValueConstraintEvaluator(new Dictionary<string, string>(StringComparer.Ordinal));
        var constraint = CreateConstraint(
            ConstraintKinds.Min,
            ConstraintPropertyNames.Min,
            JsonSerializer.SerializeToElement(10));

        var result = evaluator.IsSatisfied(ConstraintKinds.Min, constraint, "age", FormValue.FromInteger(12));

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfied_WhenPatternConstraintRejectsRawValue_ReturnsFalse()
    {
        var evaluator = new FormSetValueConstraintEvaluator(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["code"] = "abc"
            });
        var constraint = CreateConstraint(
            ConstraintKinds.Pattern,
            ConstraintPropertyNames.Pattern,
            JsonSerializer.SerializeToElement("^[0-9]+$"));

        var result = evaluator.IsSatisfied(ConstraintKinds.Pattern, constraint, "code", FormValue.FromString("abc"));

        Assert.False(result);
    }

    [Theory]
    // Das Muster unterscheidet Groß- und Kleinschreibung, damit vorgegebene Namen
    // oder Codes exakt geprüft werden können. Nur „Marta“ erfüllt dieses Muster.
    [InlineData("Marta", true)]
    [InlineData("marta", false)]
    [InlineData("MARTA", false)]
    public void TextOwner_WhenPatternConstraintIsCaseSensitive_MatchesOnlyTheExactCase(string value, bool expected)
    {
        var evaluator = new FormTextSetValueConstraintEvaluator(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = value
            });
        var constraint = CreateConstraint(
            ConstraintKinds.Pattern,
            ConstraintPropertyNames.Pattern,
            JsonSerializer.SerializeToElement("^Marta$"));

        var result = evaluator.IsSatisfied(ConstraintKinds.Pattern, constraint, "name");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TextOwner_WhenAuthoredPatternCannotBeRead_ThrowsAuthoringException()
    {
        var evaluator = new FormTextSetValueConstraintEvaluator(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["code"] = "12345"
            });
        var constraint = CreateConstraint(
            ConstraintKinds.Pattern,
            ConstraintPropertyNames.Pattern,
            JsonSerializer.SerializeToElement("[0-9"));

        Assert.Throws<FormConstraintAuthoringException>(
            () => evaluator.IsSatisfied(ConstraintKinds.Pattern, constraint, "code"));
    }

    [Fact]
    public void TextOwner_WhenMinLengthRejectsRawValue_ReturnsFalse()
    {
        var evaluator = new FormTextSetValueConstraintEvaluator(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["code"] = "ab"
            });
        var constraint = CreateConstraint(
            ConstraintKinds.MinLength,
            ConstraintPropertyNames.MinLength,
            JsonSerializer.SerializeToElement(3));

        var result = evaluator.IsSatisfied(ConstraintKinds.MinLength, constraint, "code");

        Assert.False(result);
    }

    [Fact]
    public void NumericOwner_WhenRangeMatchesIntegerValue_ReturnsTrue()
    {
        var evaluator = new FormNumericSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.Range,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ConstraintPropertyNames.Min] = JsonSerializer.SerializeToElement(10),
                [ConstraintPropertyNames.Max] = JsonSerializer.SerializeToElement(20)
            });

        var result = evaluator.IsSatisfied(ConstraintKinds.Range, constraint, FormValue.FromInteger(15));

        Assert.True(result);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void NumericOwner_WhenRangeHasBothBounds_EnforcesRange(int actual, bool expected)
    {
        var evaluator = new FormNumericSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.Range,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ConstraintPropertyNames.Min] = JsonSerializer.SerializeToElement(10),
                [ConstraintPropertyNames.Max] = JsonSerializer.SerializeToElement(20)
            });

        var result = evaluator.IsSatisfied(ConstraintKinds.Range, constraint, FormValue.FromInteger(actual));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(10, 12, true)]
    [InlineData(10, 9, false)]
    public void NumericOwner_WhenRangeHasOnlyMin_EnforcesMin(int min, int actual, bool expected)
    {
        var evaluator = new FormNumericSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.Range,
            ConstraintPropertyNames.Min,
            JsonSerializer.SerializeToElement(min));

        var result = evaluator.IsSatisfied(ConstraintKinds.Range, constraint, FormValue.FromInteger(actual));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(20, 19, true)]
    [InlineData(20, 21, false)]
    public void NumericOwner_WhenRangeHasOnlyMax_EnforcesMax(int max, int actual, bool expected)
    {
        var evaluator = new FormNumericSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.Range,
            ConstraintPropertyNames.Max,
            JsonSerializer.SerializeToElement(max));

        var result = evaluator.IsSatisfied(ConstraintKinds.Range, constraint, FormValue.FromInteger(actual));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NumericOwner_WhenRangeHasNoBounds_ReturnsTrue()
    {
        var evaluator = new FormNumericSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.Range,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal));

        var result = evaluator.IsSatisfied(ConstraintKinds.Range, constraint, FormValue.FromInteger(42));

        Assert.True(result);
    }

    [Theory]
    [InlineData(ConstraintKinds.Min, ConstraintPropertyNames.Min)]
    [InlineData(ConstraintKinds.Max, ConstraintPropertyNames.Max)]
    [InlineData(ConstraintKinds.Range, ConstraintPropertyNames.Min)]
    [InlineData(ConstraintKinds.Range, ConstraintPropertyNames.Max)]
    public void NumericOwner_WhenAuthoredBoundCannotBeParsed_ThrowsAuthoringException(
        string kind,
        string propertyName)
    {
        var evaluator = new FormNumericSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            kind,
            propertyName,
            JsonSerializer.SerializeToElement("not-an-integer"));

        Assert.Throws<FormConstraintAuthoringException>(
            () => evaluator.IsSatisfied(kind, constraint, FormValue.FromInteger(12)));
    }

    [Fact]
    public void DateOwner_WhenDateMaxRejectsDateValue_ReturnsFalse()
    {
        var evaluator = new FormDateSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.DateMax,
            ConstraintPropertyNames.MaxDate,
            JsonSerializer.SerializeToElement("2026-02-08"));

        var result = evaluator.IsSatisfied(ConstraintKinds.DateMax, constraint, FormValue.FromString("2026-02-09"));

        Assert.False(result);
    }

    [Theory]
    [InlineData("2026-02-07", false)]
    [InlineData("2026-02-08", true)]
    [InlineData("2026-02-10", true)]
    [InlineData("2026-02-11", false)]
    public void DateOwner_WhenDateRangeHasBothBounds_EnforcesRange(string actualDate, bool expected)
    {
        var evaluator = new FormDateSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.DateRange,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ConstraintPropertyNames.MinDate] = JsonSerializer.SerializeToElement("2026-02-08"),
                [ConstraintPropertyNames.MaxDate] = JsonSerializer.SerializeToElement("2026-02-10")
            });

        var result = evaluator.IsSatisfied(ConstraintKinds.DateRange, constraint, FormValue.FromString(actualDate));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-02-08", "2026-02-09", true)]
    [InlineData("2026-02-08", "2026-02-07", false)]
    public void DateOwner_WhenDateRangeHasOnlyMin_EnforcesMinDate(string minDate, string actualDate, bool expected)
    {
        var evaluator = new FormDateSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.DateRange,
            ConstraintPropertyNames.MinDate,
            JsonSerializer.SerializeToElement(minDate));

        var result = evaluator.IsSatisfied(ConstraintKinds.DateRange, constraint, FormValue.FromString(actualDate));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-02-08", "2026-02-07", true)]
    [InlineData("2026-02-08", "2026-02-09", false)]
    public void DateOwner_WhenDateRangeHasOnlyMax_EnforcesMaxDate(string maxDate, string actualDate, bool expected)
    {
        var evaluator = new FormDateSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.DateRange,
            ConstraintPropertyNames.MaxDate,
            JsonSerializer.SerializeToElement(maxDate));

        var result = evaluator.IsSatisfied(ConstraintKinds.DateRange, constraint, FormValue.FromString(actualDate));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DateOwner_WhenDateRangeHasNoBounds_ReturnsTrue()
    {
        var evaluator = new FormDateSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.DateRange,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal));

        var result = evaluator.IsSatisfied(ConstraintKinds.DateRange, constraint, FormValue.FromString("2026-02-09"));

        Assert.True(result);
    }

    [Theory]
    [InlineData(ConstraintKinds.DateMin, ConstraintPropertyNames.MinDate)]
    [InlineData(ConstraintKinds.DateMax, ConstraintPropertyNames.MaxDate)]
    [InlineData(ConstraintKinds.DateRange, ConstraintPropertyNames.MinDate)]
    [InlineData(ConstraintKinds.DateRange, ConstraintPropertyNames.MaxDate)]
    public void DateOwner_WhenAuthoredBoundCannotBeParsed_ThrowsAuthoringException(
        string kind,
        string propertyName)
    {
        var evaluator = new FormDateSetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            kind,
            propertyName,
            JsonSerializer.SerializeToElement("not-a-date"));

        Assert.Throws<FormConstraintAuthoringException>(
            () => evaluator.IsSatisfied(kind, constraint, FormValue.FromString("2026-02-09")));
    }

    [Fact]
    public void EqualityOwner_WhenNotEqualsMatchesExpectedValue_ReturnsFalse()
    {
        var evaluator = new FormEqualitySetValueConstraintEvaluator();
        var constraint = CreateConstraint(
            ConstraintKinds.NotEquals,
            ConstraintPropertyNames.Value,
            JsonSerializer.SerializeToElement("blocked"));

        var result = evaluator.IsSatisfied(ConstraintKinds.NotEquals, constraint, FormValue.FromString("blocked"));

        Assert.False(result);
    }

    private static ConstraintDefinition CreateConstraint(string kind, string propertyName, JsonElement propertyValue)
        => CreateConstraint(
            kind,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [propertyName] = propertyValue
            });

    private static ConstraintDefinition CreateConstraint(string kind, Dictionary<string, JsonElement> additionalProperties)
    {
        return new ConstraintDefinition
        {
            ConstraintId = $"constraint.{kind}",
            Kind = ConstraintKindExtensions.TryFromWireToken(kind, out var parsedKind) ? parsedKind : ConstraintKind.Unknown,
            Target = new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("value") },
            Severity = ConstraintSeverity.Error,
            Message = new LocalizedText { ["de"] = "Fehler" },
            AdditionalProperties = additionalProperties
        };
    }
}
