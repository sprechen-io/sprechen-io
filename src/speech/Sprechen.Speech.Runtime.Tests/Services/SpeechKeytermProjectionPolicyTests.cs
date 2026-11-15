using System.Text;
using Sprechen.Speech.App.Contracts.Services;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechKeytermProjectionPolicyTests
{
    private readonly SpeechKeytermProjectionPolicy _policy = new();

    [Fact]
    public void Project_TrimsDeduplicatesAndPreservesOrder()
    {
        var result = _policy.Project(["  Einfaches Beispiel  ", null, " ", "Komplexes Beispiel", "Einfaches Beispiel"]);

        Assert.Equal(["Einfaches Beispiel", "Komplexes Beispiel"], result);
    }

    [Fact]
    public void Project_MoreThanTwelveValidTerms_ReturnsFirstTwelve()
    {
        var input = Enumerable.Range(1, 13).Select(index => $"Option {index}");

        var result = _policy.Project(input);

        Assert.Equal(12, result.Count);
        Assert.Equal("Option 1", result[0]);
        Assert.Equal("Option 12", result[^1]);
    }

    [Fact]
    public void Project_OmitsTermsOutsideCharacterUtf8AndControlBounds()
    {
        var acceptedBoundary = new string('a', 128);
        var tooManyCharacters = new string('b', 129);
        var tooManyUtf8Bytes = string.Concat(Enumerable.Repeat("\u20ac", 86));

        var result = _policy.Project([acceptedBoundary, tooManyCharacters, tooManyUtf8Bytes, "line\nbreak", "valid"]);

        Assert.Equal([acceptedBoundary, "valid"], result);
        Assert.Equal(128, Encoding.UTF8.GetByteCount(result[0]));
    }

    [Fact]
    public void Project_CumulativeBudgetIsFull_OmitsFurtherTerms()
    {
        var input = Enumerable.Range(0, 5).Select(index => new string((char)('a' + index), 128));

        var result = _policy.Project(input);

        Assert.Equal(4, result.Count);
        Assert.Equal(512, result.Sum(Encoding.UTF8.GetByteCount));
    }
}
