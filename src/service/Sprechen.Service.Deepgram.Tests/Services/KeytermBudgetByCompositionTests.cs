using Sprechen.Service.Abstractions;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class KeytermBudgetByCompositionTests
{
    private readonly SpeechKeytermPolicy _policy = new();

    [Fact]
    public void Normalize_WithServerBase_StillReservesRoomBesideTheServerVocabulary()
    {
        var thirteen = Enumerable.Range(0, SpeechKeytermPolicy.MaximumCount + 1)
            .Select(index => $"term{index}")
            .ToArray();

        var exception = Assert.Throws<ArgumentException>(
            () => _policy.Normalize(new SpeechKeyterms(thirteen, KeytermComposition.WithServerBase)));

        Assert.Contains("at most 12", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Normalize_WithoutServerBase_AllowsTheWholeProviderBudget()
    {
        // Ohne Servervokabular gilt die größere Grenze für Anfragebegriffe.
        var forty = Enumerable.Range(0, 40).Select(index => $"term{index}").ToArray();

        var normalized = _policy.Normalize(new SpeechKeyterms(forty, KeytermComposition.WithoutServerBase));

        Assert.NotNull(normalized);
        Assert.Equal(40, normalized!.Terms.Count);
        Assert.Equal(KeytermComposition.WithoutServerBase, normalized.Composition);
    }

    [Fact]
    public void Normalize_WithoutServerBase_StillStopsAtTheProviderCeiling()
    {
        var overCeiling = Enumerable.Range(0, SpeechKeytermPolicy.MaximumCountWithoutServerBase + 1)
            .Select(index => $"term{index}")
            .ToArray();

        var exception = Assert.Throws<ArgumentException>(
            () => _policy.Normalize(new SpeechKeyterms(overCeiling, KeytermComposition.WithoutServerBase)));

        Assert.Contains("at most 100", exception.Message, StringComparison.Ordinal);
    }
}
