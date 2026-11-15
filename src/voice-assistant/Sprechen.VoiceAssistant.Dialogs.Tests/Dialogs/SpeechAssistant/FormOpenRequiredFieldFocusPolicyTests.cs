using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormOpenRequiredFieldFocusPolicyTests
{
    [Fact]
    public void TryFocusNextOpenRequired_MovesForwardToNextOpenRequiredAfterCurrentFocus()
    {
        var policy = CreatePolicy("a", "b", "c", "d");
        var focused = new List<string>();

        var result = policy.TryFocusNextOpenRequired("a", ["b", "d"], Focus(focused));

        Assert.Equal(FocusNextOpenRequiredFieldResultKind.Moved, result.Kind);
        Assert.Equal("b", result.TargetFieldId);
        Assert.Equal(2, result.RemainingOpenRequiredCount);
        Assert.Equal(["b"], focused);
    }

    [Fact]
    public void TryFocusNextOpenRequired_WhenCurrentFocusIsOpenRequired_SkipsItselfAndMovesToNext()
    {
        var policy = CreatePolicy("a", "b", "c");
        var focused = new List<string>();

        var result = policy.TryFocusNextOpenRequired("a", ["a", "c"], Focus(focused));

        Assert.Equal(FocusNextOpenRequiredFieldResultKind.Moved, result.Kind);
        Assert.Equal("c", result.TargetFieldId);
        Assert.Equal(["c"], focused);
    }

    [Fact]
    public void TryFocusNextOpenRequired_WhenNoOpenRequiredFollows_WrapsToFirst()
    {
        var policy = CreatePolicy("a", "b", "c", "d");
        var focused = new List<string>();

        var result = policy.TryFocusNextOpenRequired("d", ["a", "c"], Focus(focused));

        Assert.Equal(FocusNextOpenRequiredFieldResultKind.WrappedToStart, result.Kind);
        Assert.Equal("a", result.TargetFieldId);
        Assert.Equal(2, result.RemainingOpenRequiredCount);
        Assert.Equal(["a"], focused);
    }

    [Fact]
    public void TryFocusNextOpenRequired_WhenCurrentFocusIsTheSoleOpenRequired_ReportsSoleTargetWithoutMoving()
    {
        var policy = CreatePolicy("a", "b", "c");
        var focused = new List<string>();

        var result = policy.TryFocusNextOpenRequired("b", ["b"], Focus(focused));

        Assert.Equal(FocusNextOpenRequiredFieldResultKind.AlreadyOnSoleTarget, result.Kind);
        Assert.Equal("b", result.TargetFieldId);
        Assert.Equal(1, result.RemainingOpenRequiredCount);
        Assert.Empty(focused);
    }

    [Fact]
    public void TryFocusNextOpenRequired_WhenNoOpenRequired_ReportsNoneWithoutMoving()
    {
        var policy = CreatePolicy("a", "b", "c");
        var focused = new List<string>();

        var result = policy.TryFocusNextOpenRequired("a", [], Focus(focused));

        Assert.Equal(FocusNextOpenRequiredFieldResultKind.NoOpenRequiredField, result.Kind);
        Assert.Null(result.TargetFieldId);
        Assert.Equal(0, result.RemainingOpenRequiredCount);
        Assert.Empty(focused);
    }

    [Fact]
    public void TryFocusNextOpenRequired_WhenNoCurrentFocus_MovesToFirstOpenRequired()
    {
        var policy = CreatePolicy("a", "b", "c");
        var focused = new List<string>();

        var result = policy.TryFocusNextOpenRequired(focusedFieldId: null, ["b", "c"], Focus(focused));

        Assert.Equal(FocusNextOpenRequiredFieldResultKind.Moved, result.Kind);
        Assert.Equal("b", result.TargetFieldId);
        Assert.Equal(["b"], focused);
    }

    [Fact]
    public void TryFocusNextOpenRequired_SkipsOpenRequiredFieldsThatCannotBeFocused()
    {
        var policy = CreatePolicy("a", "b", "c", "d");
        var focused = new List<string>();

        var result = policy.TryFocusNextOpenRequired(
            "a",
            ["b", "c"],
            fieldId =>
            {
                focused.Add(fieldId);
                return fieldId == "c";
            });

        Assert.Equal(FocusNextOpenRequiredFieldResultKind.Moved, result.Kind);
        Assert.Equal("c", result.TargetFieldId);
        Assert.Equal(["b", "c"], focused);
    }

    private static FormOpenRequiredFieldFocusPolicy CreatePolicy(params string[] orderedFieldIds)
        => new(
            orderedFieldIds.ToImmutableArray(),
            orderedFieldIds
                .Select((fieldId, index) => KeyValuePair.Create(FieldId.From(fieldId), index))
                .ToImmutableDictionary());

    private static Func<string, bool> Focus(List<string> focused)
        => fieldId =>
        {
            focused.Add(fieldId);
            return true;
        };
}
