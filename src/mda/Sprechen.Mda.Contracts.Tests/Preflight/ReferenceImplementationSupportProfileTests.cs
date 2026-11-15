using System.Reflection;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Mda.Contracts.Tests.Preflight;

public sealed class ReferenceImplementationSupportProfileTests
{
    [Fact]
    public void SupportedFormContractConstraintKinds_MatchConstraintKindsMinusCustomConstraint()
    {
        var expected = GetPublicConstStringValues(typeof(ConstraintKinds));
        expected.Remove(ConstraintKinds.CustomConstraint);

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedFormContractConstraintKinds);
    }

    [Fact]
    public void SupportedReusableFieldConstraintKinds_RemainExpectedReusableSubset()
    {
        var expected = CreateSet(
            ConstraintKinds.Required,
            ConstraintKinds.Pattern,
            ConstraintKinds.MinLength,
            ConstraintKinds.MaxLength,
            ConstraintKinds.OneOf);

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedReusableFieldConstraintKinds);
    }

    [Fact]
    public void SupportedConstraintScopes_MatchConstraintScopes()
    {
        var expected = GetPublicConstStringValues(typeof(ConstraintScopes));

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedConstraintScopes);
    }

    [Fact]
    public void SupportedExpressionKinds_MatchExpressionKindsMinusCustom()
    {
        var expected = GetPublicConstStringValues(typeof(ExpressionKinds));
        expected.Remove(ExpressionKinds.Custom);

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedExpressionKinds);
    }

    [Fact]
    public void SupportedValueExpressionKinds_MatchValueExpressionKinds()
    {
        var expected = GetPublicConstStringValues(typeof(ValueExpressionKinds));

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedValueExpressionKinds);
    }

    [Fact]
    public void SupportedInputModes_MatchInputModes()
    {
        var expected = GetPublicConstStringValues(typeof(InputModes));

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedInputModes);
    }

    [Fact]
    public void SupportedValueTypeKinds_MatchValueTypeKindsMinusUnsupportedExtensions()
    {
        var expected = GetPublicConstStringValues(typeof(ValueTypeKinds));
        expected.Remove(ValueTypeKinds.Decimal);
        expected.Remove(ValueTypeKinds.DateTime);
        expected.Remove(ValueTypeKinds.Object);
        expected.Remove(ValueTypeKinds.List);
        expected.Remove(ValueTypeKinds.Custom);

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedValueTypeKinds);
    }

    [Fact]
    public void SupportedActionKinds_MatchActionKindsMinusCustomAction()
    {
        var expected = GetPublicConstStringValues(typeof(ActionKinds));
        expected.Remove(ActionKinds.CustomAction);

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedActionKinds);
    }

    [Fact]
    public void SupportedRuleScopes_RemainExpectedReferenceSubset()
    {
        var expected = CreateSet(
            "onInit",
            "onValueChanged",
            "onEnterFieldGroup",
            "onSubmitAttempt");

        AssertSetEqual(expected, ReferenceImplementationSupportProfile.SupportedRuleScopes);
    }

    private static HashSet<string> GetPublicConstStringValues(Type type)
    {
        return type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> CreateSet(params string[] values)
    {
        return values.ToHashSet(StringComparer.Ordinal);
    }

    private static void AssertSetEqual(HashSet<string> expected, IReadOnlySet<string> actual)
    {
        var actualSet = actual.ToHashSet(StringComparer.Ordinal);

        Assert.True(
            actualSet.SetEquals(expected),
            $"Expected set [{string.Join(", ", expected.OrderBy(value => value))}], actual [{string.Join(", ", actualSet.OrderBy(value => value))}].");
    }
}
