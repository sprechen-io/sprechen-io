using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormValidationErrorFocusPolicyTests
{
    [Fact]
    public void TryFocusNextError_WhenCurrentFieldIsLast_WrapsToEarlierViolation()
    {
        var service = CreateService("first", "second", "third");
        var focusedFields = new List<string>();

        var result = service.TryFocusNextError(
            "third",
            [CreateViolation("first", "firstRequired")],
            OrderAsProvided,
            fieldId =>
            {
                focusedFields.Add(fieldId);
                return true;
            });

        Assert.NotNull(result);
        Assert.Equal("firstRequired", result.ConstraintId);
        Assert.Equal(["first"], focusedFields);
    }

    [Fact]
    public void TryFocusNextError_WhenFocusAttemptFails_SkipsToNextViolation()
    {
        var service = CreateService("first", "second", "third");
        var focusedFields = new List<string>();

        var result = service.TryFocusNextError(
            "first",
            [CreateViolation("second", "secondRequired"), CreateViolation("third", "thirdRequired")],
            OrderAsProvided,
            fieldId =>
            {
                focusedFields.Add(fieldId);
                return fieldId == "third";
            });

        Assert.NotNull(result);
        Assert.Equal("thirdRequired", result.ConstraintId);
        Assert.Equal(["second", "third"], focusedFields);
    }

    private static FormValidationErrorFocusPolicy CreateService(params string[] orderedFieldIds)
    {
        return new FormValidationErrorFocusPolicy(
            orderedFieldIds.ToImmutableArray(),
            orderedFieldIds
                .Select((fieldId, index) => KeyValuePair.Create(FieldId.From(fieldId), index))
                .ToImmutableDictionary());
    }

    private static ImmutableArray<FormConstraintViolation> OrderAsProvided(IEnumerable<FormConstraintViolation> violations)
        => violations.ToImmutableArray();

    private static FormConstraintViolation CreateViolation(string fieldId, string constraintId)
        => new(
            fieldId,
            constraintId,
            "error",
            constraintId,
            constraintId,
            Remediation: null,
            "field",
            FieldGroupId: null);
}
