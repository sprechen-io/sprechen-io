using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Von der Referenzimplementierung unterstützte Teilmengen der Formularvokabulare.
/// Die vollständigen Modellvokabulare bleiben davon getrennt, damit nicht unterstützte
/// Merkmale ausdrücklich erkannt werden können.
/// </summary>
public static class ReferenceImplementationSupportProfile
{
    public static IReadOnlySet<string> SupportedValueTypeKinds { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        ValueTypeKinds.String,
        ValueTypeKinds.Boolean,
        ValueTypeKinds.Integer,
        ValueTypeKinds.Enum,
        ValueTypeKinds.Set,
        ValueTypeKinds.Date,
        ValueTypeKinds.Time);

    public static IReadOnlySet<string> SupportedInputModes { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        InputModes.Voice,
        InputModes.Keyboard,
        InputModes.Spelling,
        InputModes.Digits,
        InputModes.Literal);

    public static IReadOnlySet<string> SupportedFormContractConstraintKinds { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        ConstraintKinds.Required,
        ConstraintKinds.Pattern,
        ConstraintKinds.MinLength,
        ConstraintKinds.MaxLength,
        ConstraintKinds.Min,
        ConstraintKinds.Max,
        ConstraintKinds.Range,
        ConstraintKinds.DateMin,
        ConstraintKinds.DateMax,
        ConstraintKinds.DateRange,
        ConstraintKinds.Equals,
        ConstraintKinds.NotEquals,
        ConstraintKinds.OneOf,
        ConstraintKinds.AllOrNone,
        ConstraintKinds.Compare,
        ConstraintKinds.CompareVariables);

    public static IReadOnlySet<string> SupportedReusableFieldConstraintKinds { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        ConstraintKinds.Required,
        ConstraintKinds.Pattern,
        ConstraintKinds.MinLength,
        ConstraintKinds.MaxLength,
        ConstraintKinds.OneOf);

    public static IReadOnlySet<string> SupportedConstraintScopes { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        ConstraintScopes.Field,
        ConstraintScopes.FieldGroup,
        ConstraintScopes.CrossGroup,
        ConstraintScopes.Form);

    public static IReadOnlySet<string> SupportedRuleScopes { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        RuleScopes.OnInit,
        RuleScopes.OnValueChanged,
        RuleScopes.OnEnterFieldGroup,
        RuleScopes.OnSubmitAttempt);

    public static IReadOnlySet<string> SupportedExpressionKinds { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        ExpressionKinds.And,
        ExpressionKinds.Or,
        ExpressionKinds.Not,
        ExpressionKinds.Equals,
        ExpressionKinds.NotEquals,
        ExpressionKinds.Compare,
        ExpressionKinds.In,
        ExpressionKinds.Contains,
        ExpressionKinds.StartsWith,
        ExpressionKinds.IsSet,
        ExpressionKinds.IsNotSet,
        ExpressionKinds.IsEmpty,
        ExpressionKinds.IsNotEmpty);

    public static IReadOnlySet<string> SupportedValueExpressionKinds { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        ValueExpressionKinds.Var,
        ValueExpressionKinds.Const);

    public static IReadOnlySet<string> SupportedActionKinds { get; } = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        ActionKinds.SetVisible,
        ActionKinds.SetEnabled,
        ActionKinds.SetReadOnly,
        ActionKinds.SetValue,
        ActionKinds.ClearValue,
        ActionKinds.CopyValue,
        ActionKinds.NavigateToFieldGroup,
        ActionKinds.FocusField,
        ActionKinds.Speak);
}
