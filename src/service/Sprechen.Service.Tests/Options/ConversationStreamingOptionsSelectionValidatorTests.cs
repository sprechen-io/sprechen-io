using Sprechen.Service.Options;

using Xunit;

namespace Sprechen.Service.Tests.Options;

public sealed class ConversationStreamingOptionsSelectionValidatorTests
{
    [Fact]
    public void Validate_WhenDeepgramSttSelectedAndTimeoutExceedsSafeMaximum_Fails()
    {
        var validator = new ConversationStreamingOptionsSelectionValidator();

        var result = validator.Validate(
            name: null,
            new ConversationStreamingOptions
            {
                ClientInactivityTimeoutMs = ConversationStreamingOptionsSelectionValidator.DeepgramSafeMaximumClientInactivityTimeoutMs + 1
            });

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            static failure => failure.Contains(
                $"{ConversationStreamingOptions.SectionName}:ClientInactivityTimeoutMs",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenDeepgramSttSelectedAndTimeoutMatchesSafeMaximum_Passes()
    {
        var validator = new ConversationStreamingOptionsSelectionValidator();

        var result = validator.Validate(
            name: null,
            new ConversationStreamingOptions
            {
                ClientInactivityTimeoutMs = ConversationStreamingOptionsSelectionValidator.DeepgramSafeMaximumClientInactivityTimeoutMs
            });

        Assert.True(result.Succeeded);
    }
}
