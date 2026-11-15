using Sprechen.Service.Abstractions;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class SpeechKeytermPolicyTests
{
    [Fact]
    public void Normalize_PreservesOrderAndDuplicatesWhileTrimmingTerms()
    {
        var result = new SpeechKeytermPolicy().Normalize([" Alpha ", "Beta", " Alpha "]);

        Assert.Equal(["Alpha", "Beta", "Alpha"], result);
    }

    [Fact]
    public void Normalize_ExcessiveUtf8TermSize_ThrowsBeforeProviderComposition()
    {
        var multibyteTerm = new string('\u20ac', SpeechKeytermPolicy.MaximumTermLength);

        var exception = Assert.Throws<ArgumentException>(() =>
            new SpeechKeytermPolicy().Normalize([multibyteTerm]));

        Assert.Contains("UTF-8 bytes", exception.Message, StringComparison.Ordinal);
    }
}
