using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SpeechProviderOptionsSelectionValidatorTests
{
    [Fact]
    public void Validate_WhenProvidersAreDeepgram_ReturnsSuccess()
    {
        var validator = new SpeechProviderOptionsSelectionValidator();

        var result = validator.Validate(name: null, new SpeechProviderOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenProviderEnumsAreNotDeepgram_ReturnsFailures()
    {
        var validator = new SpeechProviderOptionsSelectionValidator();

        var result = validator.Validate(
            name: null,
            new SpeechProviderOptions
            {
                SttProvider = (SpeechProvider)0,
                TtsProvider = (SpeechProvider)1
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("SttProvider", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("TtsProvider", StringComparison.Ordinal));
    }
}
