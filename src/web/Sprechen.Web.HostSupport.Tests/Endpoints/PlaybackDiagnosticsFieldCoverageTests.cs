using System.Reflection;
using Sprechen.Web.HostSupport.Endpoints;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Endpoints;

/// <summary>
/// Vergleicht die serverseitige Positivliste mit den hier aufgeführten Feldern des Wiedergabeberichts.
/// Die Vergleichsliste wird von Hand gepflegt und nimmt neue Clientfelder mit auf.
/// </summary>
public sealed class PlaybackDiagnosticsFieldCoverageTests
{
    private static readonly string[] SentByTheClient =
    [
        "event",
        "utteranceSeq",
        "ts",
        "writtenSeconds",
        "playedOutSeconds",
        "underruns",
        "underrunOutputFrames",
        "silentOutputWindows",
        "maxContiguousSilentMs",
        "outputSampleRate",
        "keepAlive",
        "playbackPath",
        "streamingSinkStatus",
        "firstContentLeadMs",
        "firstContentCtxState",
        "droppedFrames",
        "starvedQuanta",
        "onsetPeakDbfs",
        "onsetFrames",
        "drainOutcome",
        "drainMs",
        "ctxState",
        "ctxStateEvents",
        "visibilityEvents",
        "sessionId",
        "clientCorrelationId",
        "operatingMode",
    ];

    private static readonly string[] SentOnlyInDiagnosticMode =
    [
        "sessionId",
        "clientCorrelationId",
        "operatingMode",
    ];

    private static string[] LoggableFields()
    {
        var field = typeof(PlaybackDiagnosticsEndpoint)
            .GetField("LoggableFields", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return Assert.IsType<string[]>(field!.GetValue(null));
    }

    [Fact]
    public void EverySentFieldIsDecidedAbout()
    {
        var allowed = LoggableFields();
        var dropped = SentByTheClient.Where(name => !allowed.Contains(name)).ToList();
        Assert.True(
            dropped.Count == 0,
            "The player sends fields the host drops: " + string.Join(", ", dropped));
    }

    [Fact]
    public void SessionModeAndRunAreLoggable_BecauseAFieldTheListDoesNotKnowIsDropped()
    {
        var allowed = LoggableFields();
        foreach (var field in SentOnlyInDiagnosticMode)
        {
            Assert.Contains(field, allowed);
        }
    }

    [Fact]
    public void TheAllowListNamesNothingTheClientDoesNotSend()
    {
        var orphaned = LoggableFields().Where(name => !SentByTheClient.Contains(name)).ToList();
        Assert.True(
            orphaned.Count == 0,
            "Allow-list names fields nobody sends: " + string.Join(", ", orphaned));
    }
}
