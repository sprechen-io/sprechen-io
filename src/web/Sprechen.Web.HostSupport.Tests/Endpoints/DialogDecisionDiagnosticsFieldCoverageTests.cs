using System.Reflection;
using Sprechen.Web.HostSupport.Endpoints;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Endpoints;

public sealed class DialogDecisionDiagnosticsFieldCoverageTests
{
    private static readonly string[] SentByTheClient =
    [
        "event",
        "name",
        "fields",
        "sessionId",
        "clientCorrelationId",
        "operatingMode",
    ];

    private static string[] LoggableFields()
    {
        var field = typeof(DialogDecisionDiagnosticsEndpoint)
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
            "Der Client sendet Felder, die serverseitig stillschweigend verschwinden: "
            + string.Join(", ", dropped));
    }

    [Fact]
    public void TheAllowListNamesNothingTheClientDoesNotSend()
    {
        var orphaned = LoggableFields().Where(name => !SentByTheClient.Contains(name)).ToList();
        Assert.True(orphaned.Count == 0, "Positivliste nennt Felder, die niemand sendet: " + string.Join(", ", orphaned));
    }
}
