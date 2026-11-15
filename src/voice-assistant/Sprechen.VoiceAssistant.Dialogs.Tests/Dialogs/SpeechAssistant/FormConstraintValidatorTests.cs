using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormConstraintValidatorTests
{
    // Standardwert für Fälle, in denen der aufgelöste Wert nicht relevant ist.
    private static readonly FormValue DefaultFormValue = default;

    #region IsConstraintSatisfied - Required

    [Fact]
    public void Required_WhenValueNotSet_ReturnsFalse()
    {
        var validator = CreateValidator();
        var constraint = CreateRequiredConstraint();

        var result = validator.IsConstraintSatisfied(constraint, "firstName", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    [Fact]
    public void Required_WhenValueSetAndNotEmpty_ReturnsTrue()
    {
        var validator = CreateValidator(("firstName", "Max"));
        var constraint = CreateRequiredConstraint();

        var result = validator.IsConstraintSatisfied(constraint, "firstName", FormValue.FromString("Max"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void Required_WhenValueSetButEmpty_ReturnsFalse()
    {
        var validator = CreateValidator(("firstName", ""));
        var constraint = CreateRequiredConstraint();

        var result = validator.IsConstraintSatisfied(constraint, "firstName", FormValue.FromString(""), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    [Fact]
    public void Required_WhenValueSetButWhitespace_ReturnsFalse()
    {
        var validator = CreateValidator(("firstName", "   "));
        var constraint = CreateRequiredConstraint();

        var result = validator.IsConstraintSatisfied(constraint, "firstName", FormValue.FromString("   "), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    [Fact]
    public void Required_WhenBooleanValueSet_ReturnsTrue()
    {
        var validator = CreateValidator(("agreed", "true"));
        var constraint = CreateRequiredConstraint();

        var result = validator.IsConstraintSatisfied(constraint, "agreed", FormValue.FromBoolean(true), isValueSet: true, valueTypeKind: ValueTypeKinds.Boolean);

        Assert.True(result);
    }

    [Fact]
    public void Required_WhenIntegerValueSet_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "42"));
        var constraint = CreateRequiredConstraint();

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(42), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void Required_WhenSetValueNonEmpty_ReturnsTrue()
    {
        var validator = CreateValidator(("colors", "red|blue"));
        var constraint = CreateRequiredConstraint();
        var setValue = FormValue.FromSet(ImmutableArray.Create("red", "blue"));

        var result = validator.IsConstraintSatisfied(constraint, "colors", setValue, isValueSet: true, valueTypeKind: ValueTypeKinds.Set);

        Assert.True(result);
    }

    [Fact]
    public void Required_WhenSetValueEmpty_ReturnsFalse()
    {
        var validator = CreateValidator(("colors", ""));
        var constraint = CreateRequiredConstraint();
        var setValue = FormValue.FromSet(ImmutableArray<string>.Empty);

        var result = validator.IsConstraintSatisfied(constraint, "colors", setValue, isValueSet: true, valueTypeKind: ValueTypeKinds.Set);

        Assert.False(result);
    }

    #endregion

    #region IsConstraintSatisfied - Pattern

    [Fact]
    public void Pattern_WhenValueNotSet_ReturnsTrue()
    {
        var validator = CreateValidator();
        var constraint = CreatePatternConstraint(@"^[A-Z][a-z]+$");

        var result = validator.IsConstraintSatisfied(constraint, "name", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void Pattern_WhenValueMatches_ReturnsTrue()
    {
        var validator = CreateValidator(("name", "Max"));
        var constraint = CreatePatternConstraint(@"^[A-Z][a-z]+$");

        var result = validator.IsConstraintSatisfied(constraint, "name", FormValue.FromString("Max"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void Pattern_WhenValueDoesNotMatch_ReturnsFalse()
    {
        var validator = CreateValidator(("name", "max"));
        var constraint = CreatePatternConstraint(@"^[A-Z][a-z]+$");

        var result = validator.IsConstraintSatisfied(constraint, "name", FormValue.FromString("max"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    [Fact]
    public void Pattern_WhenPatternMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("name", "anything"));
        var constraint = CreateConstraint(ConstraintKinds.Pattern); // Ohne pattern-Eigenschaft.

        var result = validator.IsConstraintSatisfied(constraint, "name", FormValue.FromString("anything"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void Pattern_WhenPatternEmpty_ReturnsTrue()
    {
        var validator = CreateValidator(("name", "anything"));
        var constraint = CreatePatternConstraint("");

        var result = validator.IsConstraintSatisfied(constraint, "name", FormValue.FromString("anything"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void Pattern_EmailValidation_Works()
    {
        var validator = CreateValidator(("email", "test@example.com"));
        // Die Hilfsmethode serialisiert das Muster und maskiert dabei umgekehrte Schrägstriche für JSON.
        var constraint = CreatePatternConstraint(@"^[a-zA-Z0-9.+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

        var result = validator.IsConstraintSatisfied(constraint, "email", FormValue.FromString("test@example.com"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void Pattern_EmailValidation_RejectsInvalid()
    {
        var validator = CreateValidator(("email", "not-an-email"));
        var constraint = CreatePatternConstraint(@"^[a-zA-Z0-9.+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

        var result = validator.IsConstraintSatisfied(constraint, "email", FormValue.FromString("not-an-email"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    #endregion

    #region IsConstraintSatisfied - MinLength

    [Fact]
    public void MinLength_WhenValueNotSet_ReturnsTrue()
    {
        var validator = CreateValidator();
        var constraint = CreateMinLengthConstraint(5);

        var result = validator.IsConstraintSatisfied(constraint, "password", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void MinLength_WhenValueLongerThanMin_ReturnsTrue()
    {
        var validator = CreateValidator(("password", "password123"));
        var constraint = CreateMinLengthConstraint(5);

        var result = validator.IsConstraintSatisfied(constraint, "password", FormValue.FromString("password123"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void MinLength_WhenValueExactlyMin_ReturnsTrue()
    {
        var validator = CreateValidator(("password", "12345"));
        var constraint = CreateMinLengthConstraint(5);

        var result = validator.IsConstraintSatisfied(constraint, "password", FormValue.FromString("12345"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void MinLength_WhenValueShorterThanMin_ReturnsFalse()
    {
        var validator = CreateValidator(("password", "1234"));
        var constraint = CreateMinLengthConstraint(5);

        var result = validator.IsConstraintSatisfied(constraint, "password", FormValue.FromString("1234"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    [Fact]
    public void MinLength_WhenMinLengthMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("password", "x"));
        var constraint = CreateConstraint(ConstraintKinds.MinLength); // Ohne minLength-Eigenschaft.

        var result = validator.IsConstraintSatisfied(constraint, "password", FormValue.FromString("x"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - MaxLength

    [Fact]
    public void MaxLength_WhenValueNotSet_ReturnsTrue()
    {
        var validator = CreateValidator();
        var constraint = CreateMaxLengthConstraint(10);

        var result = validator.IsConstraintSatisfied(constraint, "code", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void MaxLength_WhenValueShorterThanMax_ReturnsTrue()
    {
        var validator = CreateValidator(("code", "ABC"));
        var constraint = CreateMaxLengthConstraint(10);

        var result = validator.IsConstraintSatisfied(constraint, "code", FormValue.FromString("ABC"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void MaxLength_WhenValueExactlyMax_ReturnsTrue()
    {
        var validator = CreateValidator(("code", "ABCDEFGHIJ"));
        var constraint = CreateMaxLengthConstraint(10);

        var result = validator.IsConstraintSatisfied(constraint, "code", FormValue.FromString("ABCDEFGHIJ"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void MaxLength_WhenValueLongerThanMax_ReturnsFalse()
    {
        var validator = CreateValidator(("code", "ABCDEFGHIJK"));
        var constraint = CreateMaxLengthConstraint(10);

        var result = validator.IsConstraintSatisfied(constraint, "code", FormValue.FromString("ABCDEFGHIJK"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    [Fact]
    public void MaxLength_WhenMaxLengthMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("code", "very long value here"));
        var constraint = CreateConstraint(ConstraintKinds.MaxLength); // Ohne maxLength-Eigenschaft.

        var result = validator.IsConstraintSatisfied(constraint, "code", FormValue.FromString("very long value here"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - OneOf

    [Fact]
    public void OneOf_WhenFieldCarriesNoValue_ReturnsTrue()
    {
        // Fehlende Eingaben werden von der Pflichtregel geprüft. OneOf soll dafür keinen
        // zusätzlichen Verstoß melden und dieselbe leere Eingabe nicht doppelt zählen.
        var validator = CreateValidator();
        var constraint = CreateOneOfConstraint("red", "green", "blue");

        var result = validator.IsConstraintSatisfied(constraint, "color", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.Enum);

        Assert.True(result);
    }

    [Fact]
    public void OneOf_WhenValueInList_ReturnsTrue()
    {
        var validator = CreateValidator(("color", "green"));
        var constraint = CreateOneOfConstraint("red", "green", "blue");

        var result = validator.IsConstraintSatisfied(constraint, "color", FormValue.FromString("green"), isValueSet: true, valueTypeKind: ValueTypeKinds.Enum);

        Assert.True(result);
    }

    [Fact]
    public void OneOf_WhenValueNotInList_ReturnsFalse()
    {
        var validator = CreateValidator(("color", "yellow"));
        var constraint = CreateOneOfConstraint("red", "green", "blue");

        var result = validator.IsConstraintSatisfied(constraint, "color", FormValue.FromString("yellow"), isValueSet: true, valueTypeKind: ValueTypeKinds.Enum);

        Assert.False(result);
    }

    [Fact]
    public void OneOf_WhenValuesMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("color", "anything"));
        var constraint = CreateConstraint(ConstraintKinds.OneOf); // Ohne values-Eigenschaft.

        var result = validator.IsConstraintSatisfied(constraint, "color", FormValue.FromString("anything"), isValueSet: true, valueTypeKind: ValueTypeKinds.Enum);

        Assert.True(result);
    }

    [Fact]
    public void OneOf_WithIntegerValues_Works()
    {
        var validator = CreateValidator(("rating", "3"));
        var constraint = CreateOneOfConstraintIntegers(1, 2, 3, 4, 5);

        var result = validator.IsConstraintSatisfied(constraint, "rating", FormValue.FromInteger(3), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void OneOf_WithIntegerValueNotInList_ReturnsFalse()
    {
        var validator = CreateValidator(("rating", "6"));
        var constraint = CreateOneOfConstraintIntegers(1, 2, 3, 4, 5);

        var result = validator.IsConstraintSatisfied(constraint, "rating", FormValue.FromInteger(6), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void OneOf_WithBooleanValues_Works()
    {
        var validator = CreateValidator(("agree", "true"));
        var constraint = CreateOneOfConstraintBooleans(true);

        var result = validator.IsConstraintSatisfied(constraint, "agree", FormValue.FromBoolean(true), isValueSet: true, valueTypeKind: ValueTypeKinds.Boolean);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - Min

    [Fact]
    public void Min_WhenValueMeetsMinimum_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "42"));
        var constraint = CreateMinConstraint(18);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(42), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void Min_WhenValueBelowMinimum_ReturnsFalse()
    {
        var validator = CreateValidator(("count", "16"));
        var constraint = CreateMinConstraint(18);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(16), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void Min_WhenMinimumMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "42"));
        var constraint = CreateConstraint(ConstraintKinds.Min);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(42), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - Max

    [Fact]
    public void Max_WhenValueBelowMaximum_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "16"));
        var constraint = CreateMaxConstraint(18);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(16), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void Max_WhenValueAboveMaximum_ReturnsFalse()
    {
        var validator = CreateValidator(("count", "42"));
        var constraint = CreateMaxConstraint(18);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(42), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void Max_WhenMaximumMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "16"));
        var constraint = CreateConstraint(ConstraintKinds.Max);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(16), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - Range

    [Fact]
    public void Range_WhenValueInsideBounds_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "5"));
        var constraint = CreateRangeConstraint(1, 10);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(5), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void Range_WhenValueBelowMinimum_ReturnsFalse()
    {
        var validator = CreateValidator(("count", "0"));
        var constraint = CreateRangeConstraint(1, 10);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(0), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void Range_WhenValueAboveMaximum_ReturnsFalse()
    {
        var validator = CreateValidator(("count", "11"));
        var constraint = CreateRangeConstraint(1, 10);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(11), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    #endregion

    #region IsConstraintSatisfied - DateMin

    [Fact]
    public void DateMin_WhenDateMeetsMinimum_ReturnsTrue()
    {
        var validator = CreateValidator(("appointment", "2026-04-10"));
        var constraint = CreateDateMinConstraint("2026-04-01");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-04-10"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.True(result);
    }

    [Fact]
    public void DateMin_WhenDateBeforeMinimum_ReturnsFalse()
    {
        var validator = CreateValidator(("appointment", "2026-03-31"));
        var constraint = CreateDateMinConstraint("2026-04-01");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-03-31"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.False(result);
    }

    [Fact]
    public void DateMin_WhenDateIsInvalid_ReturnsFalse()
    {
        var validator = CreateValidator(("appointment", "31.03.2026"));
        var constraint = CreateDateMinConstraint("2026-04-01");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("31.03.2026"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.False(result);
    }

    #endregion

    #region IsConstraintSatisfied - DateMax

    [Fact]
    public void DateMax_WhenDateBelowMaximum_ReturnsTrue()
    {
        var validator = CreateValidator(("appointment", "2026-04-10"));
        var constraint = CreateDateMaxConstraint("2026-04-30");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-04-10"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.True(result);
    }

    [Fact]
    public void DateMax_WhenDateAfterMaximum_ReturnsFalse()
    {
        var validator = CreateValidator(("appointment", "2026-05-01"));
        var constraint = CreateDateMaxConstraint("2026-04-30");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-05-01"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.False(result);
    }

    [Fact]
    public void DateMax_WhenMaximumMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("appointment", "2026-04-10"));
        var constraint = CreateConstraint(ConstraintKinds.DateMax);

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-04-10"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - DateRange

    [Fact]
    public void DateRange_WhenDateInsideBounds_ReturnsTrue()
    {
        var validator = CreateValidator(("appointment", "2026-04-15"));
        var constraint = CreateDateRangeConstraint("2026-04-01", "2026-04-30");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-04-15"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.True(result);
    }

    [Fact]
    public void DateRange_WhenDateBeforeMinimum_ReturnsFalse()
    {
        var validator = CreateValidator(("appointment", "2026-03-31"));
        var constraint = CreateDateRangeConstraint("2026-04-01", "2026-04-30");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-03-31"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.False(result);
    }

    [Fact]
    public void DateRange_WhenDateAtBoundary_ReturnsTrue()
    {
        var validator = CreateValidator(("appointment", "2026-04-30"));
        var constraint = CreateDateRangeConstraint("2026-04-01", "2026-04-30");

        var result = validator.IsConstraintSatisfied(constraint, "appointment", FormValue.FromString("2026-04-30"), isValueSet: true, valueTypeKind: ValueTypeKinds.Date);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - Equals

    [Fact]
    public void Equals_WhenValueMatches_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "42"));
        var constraint = CreateValueConstraint(ConstraintKinds.Equals, "42");

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(42), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void Equals_WhenValueDiffers_ReturnsFalse()
    {
        var validator = CreateValidator(("count", "41"));
        var constraint = CreateValueConstraint(ConstraintKinds.Equals, "42");

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(41), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void Equals_WhenValuePropertyMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "41"));
        var constraint = CreateConstraint(ConstraintKinds.Equals);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(41), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - NotEquals

    [Fact]
    public void NotEquals_WhenValueMatches_ReturnsFalse()
    {
        var validator = CreateValidator(("count", "42"));
        var constraint = CreateValueConstraint(ConstraintKinds.NotEquals, "42");

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(42), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void NotEquals_WhenValueDiffers_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "41"));
        var constraint = CreateValueConstraint(ConstraintKinds.NotEquals, "42");

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(41), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void NotEquals_WhenValuePropertyMissing_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "41"));
        var constraint = CreateConstraint(ConstraintKinds.NotEquals);

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(41), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    #endregion

    #region IsConstraintSatisfied - Compare

    [Fact]
    public void Compare_WhenOperatorMatches_ReturnsTrue()
    {
        var validator = CreateValidator(("count", "42"));
        var constraint = CreateCompareConstraint("gte", "42");

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(42), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void Compare_WhenOperatorDoesNotMatch_ReturnsFalse()
    {
        var validator = CreateValidator(("count", "41"));
        var constraint = CreateCompareConstraint("gt", "42");

        var result = validator.IsConstraintSatisfied(constraint, "count", FormValue.FromInteger(41), isValueSet: true, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void Compare_WhenValueIsNotInteger_ReturnsFalse()
    {
        var validator = CreateValidator(("name", "Max"));
        var constraint = CreateCompareConstraint("eq", "\"Max\"");

        var result = validator.IsConstraintSatisfied(constraint, "name", FormValue.FromString("Max"), isValueSet: true, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    #endregion

    #region IsConstraintSatisfied - CompareVariables

    [Fact]
    public void CompareVariables_WhenComparisonMatches_ReturnsTrue()
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("left", FormValue.FromInteger(10)),
            ("right", FormValue.FromInteger(5)));
        var constraint = CreateCompareVariablesConstraint("left", "right", "gt");

        var result = validator.IsConstraintSatisfied(constraint, "left", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    [Fact]
    public void CompareVariables_WhenComparisonFails_ReturnsFalse()
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("left", FormValue.FromInteger(5)),
            ("right", FormValue.FromInteger(10)));
        var constraint = CreateCompareVariablesConstraint("left", "right", "gt");

        var result = validator.IsConstraintSatisfied(constraint, "left", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    [Fact]
    public void CompareVariables_WhenOneValueMissing_ReturnsTrue()
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("left", FormValue.FromInteger(5)));
        var constraint = CreateCompareVariablesConstraint("left", "right", "gt");

        var result = validator.IsConstraintSatisfied(constraint, "left", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.Integer);

        Assert.True(result);
    }

    // Datumswerte werden als ISO-Zeichenketten yyyy-MM-dd übergeben.
    // Die feldübergreifende Prüfung vergleicht ihre zeitliche Reihenfolge.

    [Theory]
    [InlineData("2026-04-10", "2026-04-01", "gt", true)]   // Ende nach Beginn.
    [InlineData("2026-04-01", "2026-04-10", "gt", false)]  // Ende vor Beginn verletzt die Regel.
    [InlineData("2026-04-10", "2026-04-01", "gte", true)]
    [InlineData("2026-04-01", "2026-04-01", "gte", true)]
    [InlineData("2026-04-01", "2026-04-10", "lt", true)]
    [InlineData("2026-04-01", "2026-04-01", "eq", true)]
    [InlineData("2026-04-01", "2026-04-02", "eq", false)]
    [InlineData("2026-04-01", "2026-04-02", "neq", true)]
    public void CompareVariables_WhenBothOperandsAreDates_ComparesByOrdinal(
        string leftDate,
        string rightDate,
        string operatorValue,
        bool expected)
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("endDate", FormValue.FromString(leftDate)),
            ("startDate", FormValue.FromString(rightDate)));
        var constraint = CreateCompareVariablesConstraint("endDate", "startDate", operatorValue);

        var result = validator.IsConstraintSatisfied(constraint, "endDate", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.Date);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CompareVariables_WhenOneDateOperandMissing_ReturnsTrue()
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("endDate", FormValue.FromString("2026-04-10")));
        var constraint = CreateCompareVariablesConstraint("endDate", "startDate", "gt");

        var result = validator.IsConstraintSatisfied(constraint, "endDate", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.Date);

        Assert.True(result);
    }

    [Theory]
    [InlineData("2026-04-10", "not-a-date")]   // Datum und sonstiger Text sind hier nicht vergleichbar.
    [InlineData("hello", "world")]             // Zwei gewöhnliche Texte sind hier nicht vergleichbar.
    public void CompareVariables_WhenOperandsAreNotComparable_ReturnsFalse(
        string leftValue,
        string rightValue)
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("left", FormValue.FromString(leftValue)),
            ("right", FormValue.FromString(rightValue)));
        var constraint = CreateCompareVariablesConstraint("left", "right", "gt");

        var result = validator.IsConstraintSatisfied(constraint, "left", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    [Fact]
    public void CompareVariables_WhenOperandsMixIntegerAndDate_ReturnsFalse()
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("left", FormValue.FromInteger(20260410)),
            ("right", FormValue.FromString("2026-04-01")));
        var constraint = CreateCompareVariablesConstraint("left", "right", "gt");

        var result = validator.IsConstraintSatisfied(constraint, "left", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.Integer);

        Assert.False(result);
    }

    #endregion

    #region IsConstraintSatisfied - AllOrNone

    [Fact]
    public void AllOrNone_WhenNoValuesAreSet_ReturnsTrue()
    {
        var validator = CreateValidator();
        var constraint = CreateAllOrNoneConstraint("a", "b", "c");

        var result = validator.IsConstraintSatisfied(constraint, "a", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void AllOrNone_WhenAllValuesAreSet_ReturnsTrue()
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("a", FormValue.FromString("one")),
            ("b", FormValue.FromString("two")),
            ("c", FormValue.FromString("three")));
        var constraint = CreateAllOrNoneConstraint("a", "b", "c");

        var result = validator.IsConstraintSatisfied(constraint, "a", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.True(result);
    }

    [Fact]
    public void AllOrNone_WhenOnlySomeValuesAreSet_ReturnsFalse()
    {
        var validator = CreateValidatorWithResolvedValues(
            [],
            ("a", FormValue.FromString("one")),
            ("b", FormValue.FromString("two")));
        var constraint = CreateAllOrNoneConstraint("a", "b", "c");

        var result = validator.IsConstraintSatisfied(constraint, "a", DefaultFormValue, isValueSet: false, valueTypeKind: ValueTypeKinds.String);

        Assert.False(result);
    }

    #endregion

    #region IsConstraintSatisfied - Unknown kind

    [Fact]
    public void UnknownKind_ThrowsInvalidOperationException()
    {
        var validator = CreateValidator(("field", "value"));
        var constraint = CreateConstraint("unknownConstraintKind");

        Assert.Throws<InvalidOperationException>(() => validator.IsConstraintSatisfied(
            constraint,
            "field",
            FormValue.FromString("value"),
            isValueSet: true,
            valueTypeKind: ValueTypeKinds.String));
    }

    [Fact]
    public void CustomConstraint_ThrowsInvalidOperationException()
    {
        var validator = CreateValidator(("field", "value"));
        var constraint = CreateCustomConstraint();

        Assert.Throws<InvalidOperationException>(() => validator.IsConstraintSatisfied(
            constraint,
            "field",
            FormValue.FromString("value"),
            isValueSet: true,
            valueTypeKind: ValueTypeKinds.String));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOrWhitespaceKind_ThrowsInvalidOperationException(string? kind)
    {
        var validator = CreateValidator(("field", "value"));
        var constraint = CreateConstraint(kind);

        Assert.Throws<InvalidOperationException>(() => validator.IsConstraintSatisfied(
            constraint,
            "field",
            FormValue.FromString("value"),
            isValueSet: true,
            valueTypeKind: ValueTypeKinds.String));
    }

    #endregion

    #region SeverityRank

    [Fact]
    public void SeverityRank_Error_Returns3()
    {
        Assert.Equal(3, FormConstraintValidator.SeverityRank(SeverityLevels.Error));
    }

    [Fact]
    public void SeverityRank_Warning_Returns2()
    {
        Assert.Equal(2, FormConstraintValidator.SeverityRank(SeverityLevels.Warning));
    }

    [Fact]
    public void SeverityRank_Info_Returns1()
    {
        Assert.Equal(1, FormConstraintValidator.SeverityRank(SeverityLevels.Info));
    }

    [Fact]
    public void SeverityRank_Unknown_Returns0()
    {
        Assert.Equal(0, FormConstraintValidator.SeverityRank("unknown"));
    }

    [Fact]
    public void SeverityRank_CaseInsensitive()
    {
        Assert.Equal(3, FormConstraintValidator.SeverityRank("ERROR"));
        Assert.Equal(2, FormConstraintValidator.SeverityRank("Warning"));
        Assert.Equal(1, FormConstraintValidator.SeverityRank("INFO"));
    }

    #endregion

    #region IsConstraintRelevantToField

    [Fact]
    public void IsConstraintRelevantToField_WhenTargetKindIsField_AndFieldIdMatches_ReturnsTrue()
    {
        var constraint = CreateConstraintWithTarget(TargetKinds.Field, "firstNameField", null);

        var result = FormConstraintValidator.IsConstraintRelevantToField(constraint, "firstNameField", "firstNameVar");

        Assert.True(result);
    }

    [Fact]
    public void IsConstraintRelevantToField_WhenTargetKindIsField_AndFieldIdDiffers_ReturnsFalse()
    {
        var constraint = CreateConstraintWithTarget(TargetKinds.Field, "lastNameField", null);

        var result = FormConstraintValidator.IsConstraintRelevantToField(constraint, "firstNameField", "firstNameVar");

        Assert.False(result);
    }

    [Fact]
    public void IsConstraintRelevantToField_WhenTargetKindIsVariable_AndVariableIdMatches_ReturnsTrue()
    {
        var constraint = CreateConstraintWithTarget(TargetKinds.Variable, null, "firstNameVar");

        var result = FormConstraintValidator.IsConstraintRelevantToField(constraint, "firstNameField", "firstNameVar");

        Assert.True(result);
    }

    [Fact]
    public void IsConstraintRelevantToField_WhenTargetKindIsVariable_AndVariableIdDiffers_ReturnsFalse()
    {
        var constraint = CreateConstraintWithTarget(TargetKinds.Variable, null, "lastNameVar");

        var result = FormConstraintValidator.IsConstraintRelevantToField(constraint, "firstNameField", "firstNameVar");

        Assert.False(result);
    }

    [Fact]
    public void IsConstraintRelevantToField_WhenTargetKindIsForm_ReturnsTrue()
    {
        var constraint = CreateConstraintWithTarget(TargetKinds.Form, null, null);

        var result = FormConstraintValidator.IsConstraintRelevantToField(constraint, "firstNameField", "firstNameVar");

        Assert.True(result);
    }

    [Fact]
    public void IsConstraintRelevantToField_WhenTargetKindIsUnknown_ReturnsFalse()
    {
        var constraint = CreateConstraintWithTarget("unsupportedKind", null, null);

        var result = FormConstraintValidator.IsConstraintRelevantToField(constraint, "fieldId", "variableId");

        Assert.False(result);
    }

    #endregion

    #region Test Helpers

    private static FormConstraintValidator CreateValidator(params (string variableId, string rawValue)[] values)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (variableId, rawValue) in values)
        {
            dict[variableId] = rawValue;
        }

        return new FormConstraintValidator(dict, _ => null);
    }

    private static FormConstraintValidator CreateValidatorWithResolvedValues(
        (string variableId, string rawValue)[] rawValues,
        params (string variableId, FormValue value)[] resolvedValues)
    {
        var rawValuesByVariableId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (variableId, rawValue) in rawValues)
        {
            rawValuesByVariableId[variableId] = rawValue;
        }

        var resolvedValuesByVariableId = new Dictionary<string, FormValue>(StringComparer.Ordinal);
        foreach (var (variableId, value) in resolvedValues)
        {
            resolvedValuesByVariableId[variableId] = value;
        }

        return new FormConstraintValidator(
            rawValuesByVariableId,
            variableId => resolvedValuesByVariableId.TryGetValue(variableId, out var value) ? value : null);
    }

    private static ConstraintDefinition CreateRequiredConstraint()
    {
        return CreateConstraint(ConstraintKinds.Required);
    }

    private static ConstraintDefinition CreatePatternConstraint(string pattern)
    {
        var props = new Dictionary<string, JsonElement>
        {
            ["pattern"] = JsonSerializer.SerializeToDocument(pattern).RootElement
        };
        return CreateConstraint(ConstraintKinds.Pattern, props);
    }

    private static ConstraintDefinition CreateMinLengthConstraint(int minLength)
    {
        var props = new Dictionary<string, JsonElement>
        {
            ["minLength"] = JsonDocument.Parse(minLength.ToString()).RootElement
        };
        return CreateConstraint(ConstraintKinds.MinLength, props);
    }

    private static ConstraintDefinition CreateMaxLengthConstraint(int maxLength)
    {
        var props = new Dictionary<string, JsonElement>
        {
            ["maxLength"] = JsonDocument.Parse(maxLength.ToString()).RootElement
        };
        return CreateConstraint(ConstraintKinds.MaxLength, props);
    }

    private static ConstraintDefinition CreateOneOfConstraint(params string[] values)
    {
        var json = $"[{string.Join(",", values.Select(v => $"\"{v}\""))}]";
        var props = new Dictionary<string, JsonElement>
        {
            ["values"] = JsonDocument.Parse(json).RootElement
        };
        return CreateConstraint(ConstraintKinds.OneOf, props);
    }

    private static ConstraintDefinition CreateOneOfConstraintIntegers(params int[] values)
    {
        var json = $"[{string.Join(",", values)}]";
        var props = new Dictionary<string, JsonElement>
        {
            ["values"] = JsonDocument.Parse(json).RootElement
        };
        return CreateConstraint(ConstraintKinds.OneOf, props);
    }

    private static ConstraintDefinition CreateOneOfConstraintBooleans(params bool[] values)
    {
        var json = $"[{string.Join(",", values.Select(v => v ? "true" : "false"))}]";
        var props = new Dictionary<string, JsonElement>
        {
            ["values"] = JsonDocument.Parse(json).RootElement
        };
        return CreateConstraint(ConstraintKinds.OneOf, props);
    }

    private static ConstraintDefinition CreateMinConstraint(int min)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Min] = ParseJsonElement(min.ToString())
        };
        return CreateConstraint(ConstraintKinds.Min, props);
    }

    private static ConstraintDefinition CreateMaxConstraint(int max)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Max] = ParseJsonElement(max.ToString())
        };
        return CreateConstraint(ConstraintKinds.Max, props);
    }

    private static ConstraintDefinition CreateRangeConstraint(int min, int max)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Min] = ParseJsonElement(min.ToString()),
            [ConstraintPropertyNames.Max] = ParseJsonElement(max.ToString())
        };
        return CreateConstraint(ConstraintKinds.Range, props);
    }

    private static ConstraintDefinition CreateDateMinConstraint(string minDate)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.MinDate] = ParseJsonElement($"\"{minDate}\"")
        };
        return CreateConstraint(ConstraintKinds.DateMin, props);
    }

    private static ConstraintDefinition CreateDateMaxConstraint(string maxDate)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.MaxDate] = ParseJsonElement($"\"{maxDate}\"")
        };
        return CreateConstraint(ConstraintKinds.DateMax, props);
    }

    private static ConstraintDefinition CreateDateRangeConstraint(string minDate, string maxDate)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.MinDate] = ParseJsonElement($"\"{minDate}\""),
            [ConstraintPropertyNames.MaxDate] = ParseJsonElement($"\"{maxDate}\"")
        };
        return CreateConstraint(ConstraintKinds.DateRange, props);
    }

    private static ConstraintDefinition CreateValueConstraint(string kind, string jsonLiteral)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Value] = ParseJsonElement(jsonLiteral)
        };
        return CreateConstraint(kind, props);
    }

    private static ConstraintDefinition CreateCompareConstraint(string operatorValue, string jsonLiteral)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.Operator] = ParseJsonElement($"\"{operatorValue}\""),
            [ConstraintPropertyNames.Value] = ParseJsonElement(jsonLiteral)
        };
        return CreateConstraint(ConstraintKinds.Compare, props);
    }

    private static ConstraintDefinition CreateCompareVariablesConstraint(string leftVariableId, string rightVariableId, string operatorValue)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.LeftVariableId] = ParseJsonElement($"\"{leftVariableId}\""),
            [ConstraintPropertyNames.RightVariableId] = ParseJsonElement($"\"{rightVariableId}\""),
            [ConstraintPropertyNames.Operator] = ParseJsonElement($"\"{operatorValue}\"")
        };
        return CreateConstraint(ConstraintKinds.CompareVariables, props);
    }

    private static ConstraintDefinition CreateAllOrNoneConstraint(params string[] variableIds)
    {
        var json = $"[{string.Join(",", variableIds.Select(variableId => $"\"{variableId}\""))}]";
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.VariableIds] = ParseJsonElement(json)
        };
        return CreateConstraint(ConstraintKinds.AllOrNone, props);
    }

    private static ConstraintDefinition CreateCustomConstraint()
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ConstraintPropertyNames.CustomConstraintName] = ParseJsonElement("\"customRule\"")
        };
        return CreateConstraint(ConstraintKinds.CustomConstraint, props);
    }

    private static ConstraintDefinition CreateConstraint(string? kind, Dictionary<string, JsonElement>? additionalProperties = null)
    {
        return new ConstraintDefinition
        {
            ConstraintId = "test-constraint",
            Kind = ConstraintKindExtensions.TryFromWireToken(kind, out var parsedKind) ? parsedKind : ConstraintKind.Unknown,
            Target = new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("testVar") },
            Severity = ConstraintSeverity.Error,
            AdditionalProperties = additionalProperties
        };
    }

    private static ConstraintDefinition CreateConstraintWithTarget(string targetKind, string? fieldId, string? variableId)
    {
        return new ConstraintDefinition
        {
            ConstraintId = "test-constraint",
            Kind = ConstraintKind.Required,
            Target = new ConstraintTargetRef { Kind = targetKind, FieldId = string.IsNullOrWhiteSpace(fieldId) ? null : FieldId.From(fieldId), VariableId = string.IsNullOrWhiteSpace(variableId) ? null : VariableId.From(variableId) },
            Severity = ConstraintSeverity.Error
        };
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    #endregion
}

