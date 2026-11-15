using System.Reflection;
using Sprechen.Web.HostSupport.Endpoints;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Endpoints;

/// <summary>
/// Vergleicht die serverseitige Positivliste mit den hier aufgeführten Clientfeldern.
/// Die Vergleichsliste wird von Hand gepflegt und nimmt neue Clientfelder mit auf.
/// Die bewusste Auswahl je Feld begrenzt den Inhalt der Betriebsprotokolle.
/// Da die Übertragung per JSON ohne Referenz auf die Speech-Verträge erfolgt,
/// prüft CaptureDiagnosticsAdmissionTests die Sendeseite gesondert.
/// </summary>
public sealed class CaptureDiagnosticsFieldCoverageTests
{

    private static readonly string[] SentByTheClient =
    [
        "event",
        "captureSeq",
        "ts",
        "turnId",
        "outcome",
        "armedToOutcomeMs",
        "captureMode",
        "startContext",
        "capturedChunks",
        "peakAmplitude",
        "estimatedFloor",
        "appliedOnsetThreshold",
        "faultKind",
        "observedFrames",
        "startCursorHandedOff",
        "lookbackBytes",
        "echoCancellation",
        "noiseSuppression",
        "autoGainControl",
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
        var field = typeof(CaptureDiagnosticsEndpoint)
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
            + string.Join(", ", dropped)
            + ". Entweder aufnehmen oder hier ausdruecklich ausschliessen - genau dieses Vergessen "
            + "machte den Vorfall vom 2026-08-28 undiagnostizierbar.");
    }

    [Fact]
    public void TheFaultKindIsLoggable_BecauseItIsTheOneFieldThatExplainsAFailure()
    {
        Assert.Contains("faultKind", LoggableFields());
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

        Assert.True(orphaned.Count == 0, "Positivliste nennt Felder, die niemand sendet: " + string.Join(", ", orphaned));
    }
}
