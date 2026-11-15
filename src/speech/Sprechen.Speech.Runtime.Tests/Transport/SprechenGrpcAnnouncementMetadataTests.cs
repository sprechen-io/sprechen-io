using Sprechen.Proto.Transport;
using Sprechen.Speech.App.Contracts.Services;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.Transport;

/// <summary>
/// Gleicht die Enum-Werte mit ausdrücklich festgelegten Headerwerten ab und
/// prüft die aufgeführten Übertragungswerte sowie den Rückfall auf Prompt.
/// </summary>
public sealed class SprechenGrpcAnnouncementMetadataTests
{
    /// <summary>
    /// Erwartete Headerwerte als Literale, damit eine Umbenennung im Enum
    /// nicht zugleich die Erwartung des Tests verändert.
    /// </summary>
    private static readonly IReadOnlyDictionary<SpokenAnnouncementKind, string> ExpectedWireValues =
        new Dictionary<SpokenAnnouncementKind, string>
        {
            [SpokenAnnouncementKind.Prompt] = "prompt",
            [SpokenAnnouncementKind.NoInput] = "noinput",
            [SpokenAnnouncementKind.RecoverableFailure] = "recoverablefailure",
        };

    [Fact]
    public void EveryAnnouncementKind_HasAPinnedWireSpelling()
    {
        // Neue Enum-Werte benötigen hier eine ausdrückliche Erwartung.
        Assert.Equal(
            Enum.GetValues<SpokenAnnouncementKind>().Order(),
            ExpectedWireValues.Keys.Order());
    }

    [Theory]
    [InlineData(SpokenAnnouncementKind.Prompt)]
    [InlineData(SpokenAnnouncementKind.NoInput)]
    [InlineData(SpokenAnnouncementKind.RecoverableFailure)]
    public void TheHeaderCarriesThePinnedSpelling(SpokenAnnouncementKind announcementKind)
    {
        var headers = SprechenGrpcAnnouncementMetadata.CreateHeaders(announcementKind.ToString());

        Assert.Equal(
            ExpectedWireValues[announcementKind],
            SprechenGrpcAnnouncementMetadata.ReadAnnouncementKind(headers));
    }

    [Fact]
    public void AMissingHeaderReadsAsAnOrdinaryPrompt()
    {
        // Aufrufe ohne Header sollen weiterhin als gewöhnliche Prompts gelten.
        Assert.Equal(
            ExpectedWireValues[SpokenAnnouncementKind.Prompt],
            SprechenGrpcAnnouncementMetadata.ReadAnnouncementKind(headers: null));
    }

    [Fact]
    public void AnUnknownSpellingReadsAsAnOrdinaryPrompt()
    {
        // Unbekannte Angaben sollen keine zusätzlichen Kategorien erzeugen.
        var headers = SprechenGrpcAnnouncementMetadata.CreateHeaders("somethingNobodyDeclared");

        Assert.Equal(
            ExpectedWireValues[SpokenAnnouncementKind.Prompt],
            SprechenGrpcAnnouncementMetadata.ReadAnnouncementKind(headers));
    }
}
