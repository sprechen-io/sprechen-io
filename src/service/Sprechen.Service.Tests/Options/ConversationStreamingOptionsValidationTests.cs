using System.ComponentModel.DataAnnotations;

using Sprechen.Service.Options;

using Xunit;

namespace Sprechen.Service.Tests.Options;

public sealed class ConversationStreamingOptionsValidationTests
{
    [Fact]
    public void ClientInactivityTimeoutMs_WhenSetToThirtySeconds_PassesDataAnnotationsValidation()
    {
        var options = new ConversationStreamingOptions
        {
            ClientInactivityTimeoutMs = 30_000
        };

        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void ClientInactivityTimeoutMs_WhenAboveThirtySeconds_FailsDataAnnotationsValidation()
    {
        var options = new ConversationStreamingOptions
        {
            ClientInactivityTimeoutMs = 30_001
        };

        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(
            validationResults,
            static result => string.Equals(
                result.MemberNames.SingleOrDefault(),
                nameof(ConversationStreamingOptions.ClientInactivityTimeoutMs),
                StringComparison.Ordinal));
    }
}
