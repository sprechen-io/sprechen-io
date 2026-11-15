using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeUtteranceStitcherTests
{
    private static readonly DateTimeOffset Anchor = new(2026, 4, 19, 10, 0, 0, TimeSpan.Zero);

    private static WakeWordInventory Inventory(params string[] forms)
        => new(forms, [], notRecognizedEarconId: null);

    private static WakeUtteranceWindow Window(int maxPauseMs = 1500, int maxCarried = 1, bool prefixOnly = true)
        => new()
        {
            MaxInterCyclePauseMs = maxPauseMs,
            MaxCarriedCycles = maxCarried,
            PrefixOnlyFirstCycleAccepted = prefixOnly
        };

    [Fact]
    public void AttachOrStart_NullWindow_ReturnsNoOp()
    {
        var stitcher = new WakeUtteranceStitcher();

        var result = stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, Inventory("okay hilfe"), window: null);

        Assert.IsType<WakeStitchNoOpResult>(result);
        Assert.Null(stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void AttachOrStart_MaxCarriedZero_ReturnsNoOp()
    {
        var stitcher = new WakeUtteranceStitcher();
        var window = Window(maxCarried: 0);

        var result = stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, Inventory("okay hilfe"), window);

        Assert.IsType<WakeStitchNoOpResult>(result);
    }

    [Fact]
    public void AttachOrStart_FirstCycleNonPrefix_PrefixOnlyTrue_ReturnsNoOp()
    {
        var stitcher = new WakeUtteranceStitcher();

        var result = stitcher.AttachOrStart(new SpeechTranscript("Wetter", 90), Anchor, Inventory("okay hilfe"), Window());

        Assert.IsType<WakeStitchNoOpResult>(result);
        Assert.Null(stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void AttachOrStart_FirstCycleValidPrefix_ReturnsStartedAndCarriesState()
    {
        var stitcher = new WakeUtteranceStitcher();

        var result = stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, Inventory("okay hilfe"), Window());

        var started = Assert.IsType<WakeStitchStartedResult>(result);
        Assert.Equal("Okay", started.TranscriptText);
        Assert.Equal("Okay", stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void AttachOrStart_SecondCycleInWindow_StitchedMatch_ReturnsAttachedAndClearsState()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window();
        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        var result = stitcher.AttachOrStart(new SpeechTranscript("Hilfe", 94), Anchor.AddMilliseconds(800), inventory, window);

        var attached = Assert.IsType<WakeStitchAttachedResult>(result);
        Assert.Equal("Okay Hilfe", attached.StitchedTranscriptText);
        Assert.Equal(string.Empty, attached.CommandText);
        Assert.Equal(WakeWordActivationStatus.LeadingWakeWordDetected, attached.Evaluation.Status);
        Assert.Null(stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void AttachOrStart_StitchedRepeatedActivation_ReturnsEmptyCommandText()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("hey alex", "okay alex");
        var window = Window();
        stitcher.AttachOrStart(new SpeechTranscript("Hey", 80), Anchor, inventory, window);

        var result = stitcher.AttachOrStart(
            new SpeechTranscript("Alex. Okay Alex.", 94),
            Anchor.AddMilliseconds(800),
            inventory,
            window);

        var attached = Assert.IsType<WakeStitchAttachedResult>(result);
        Assert.Equal("Hey Alex. Okay Alex.", attached.StitchedTranscriptText);
        Assert.Equal(string.Empty, attached.CommandText);
        Assert.Equal(WakeWordActivationStatus.LeadingWakeWordDetected, attached.Evaluation.Status);
    }

    [Fact]
    public void AttachOrStart_SecondCycleOutOfWindow_ReturnsExpiredAndClearsState()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window(maxPauseMs: 500);
        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        var result = stitcher.AttachOrStart(new SpeechTranscript("Hilfe", 94), Anchor.AddMilliseconds(900), inventory, window);

        var expired = Assert.IsType<WakeStitchExpiredResult>(result);
        Assert.Equal("Okay", expired.PreviousTranscriptText);
        Assert.Null(stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void AttachOrStart_SecondCycleInWindowNoMatch_ReturnsNoOpAndClearsState()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window();
        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        var result = stitcher.AttachOrStart(new SpeechTranscript("Wetter", 70), Anchor.AddMilliseconds(500), inventory, window);

        Assert.IsType<WakeStitchNoOpResult>(result);
        Assert.Null(stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void AttachOrStart_EmptyTranscriptWithoutCarried_ReturnsNoOp()
    {
        var stitcher = new WakeUtteranceStitcher();

        var result = stitcher.AttachOrStart(new SpeechTranscript("   ", null), Anchor, Inventory("okay hilfe"), Window());

        Assert.IsType<WakeStitchNoOpResult>(result);
    }

    [Fact]
    public void AttachOrStart_EmptyTranscriptWithCarried_OutOfWindow_ReturnsExpired()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window(maxPauseMs: 500);
        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        var result = stitcher.AttachOrStart(new SpeechTranscript(" ", null), Anchor.AddMilliseconds(900), inventory, window);

        var expired = Assert.IsType<WakeStitchExpiredResult>(result);
        Assert.Equal("Okay", expired.PreviousTranscriptText);
    }

    [Fact]
    public void AttachOrStart_EmptyTranscriptWithCarried_InWindow_ReturnsNoOpAndKeepsState()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window();
        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        var result = stitcher.AttachOrStart(new SpeechTranscript("", null), Anchor.AddMilliseconds(200), inventory, window);

        Assert.IsType<WakeStitchNoOpResult>(result);
        Assert.Equal("Okay", stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void Reset_ClearsCarriedState()
    {
        var stitcher = new WakeUtteranceStitcher();
        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, Inventory("okay hilfe"), Window());
        Assert.NotNull(stitcher.CarriedTranscriptText);

        stitcher.Reset();

        Assert.Null(stitcher.CarriedTranscriptText);
    }

    [Fact]
    public void AttachOrStart_FailedStitchWithEligibleSecondCycle_ReSeedsAsFreshCarry()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window();

        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        // Das zweite „Okay“ bleibt als neuer Anfang erhalten, obwohl „Okay Okay“ nicht aktiviert.
        var reseed = stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor.AddMilliseconds(400), inventory, window);

        Assert.IsType<WakeStitchStartedResult>(reseed);
        Assert.Equal("Okay", stitcher.CarriedTranscriptText);

        var attached = stitcher.AttachOrStart(new SpeechTranscript("Hilfe", 94), Anchor.AddMilliseconds(800), inventory, window);

        Assert.IsType<WakeStitchAttachedResult>(attached);
    }

    [Fact]
    public void AttachOrStart_PauseWindowIsMeasuredFromTheSuppliedCaptureTimestamps()
    {
        // Maßgeblich sind die übergebenen Aufnahmezeitpunkte, nicht die Laufzeit des Tests.
        var inventory = Inventory("okay hilfe");
        var window = Window(maxPauseMs: 850);

        var withinStitcher = new WakeUtteranceStitcher();
        withinStitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);
        var within = withinStitcher.AttachOrStart(new SpeechTranscript("Hilfe", 94), Anchor.AddMilliseconds(800), inventory, window);

        var pastStitcher = new WakeUtteranceStitcher();
        pastStitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);
        var past = pastStitcher.AttachOrStart(new SpeechTranscript("Wetter", 70), Anchor.AddMilliseconds(900), inventory, window);

        Assert.IsType<WakeStitchAttachedResult>(within);
        Assert.IsType<WakeStitchExpiredResult>(past);
    }

    [Fact]
    public void AttachOrStart_ExpiredCarryWithEligibleCurrentCycle_ReSeedsThenActivates()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window(maxPauseMs: 850);

        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        // Ein verspätetes „Okay“ ersetzt den abgelaufenen Anfang, damit danach „Hilfe“ anschließen kann.
        var reseed = stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor.AddMilliseconds(2000), inventory, window);

        Assert.IsType<WakeStitchStartedResult>(reseed);
        Assert.Equal("Okay", stitcher.CarriedTranscriptText);

        var attached = stitcher.AttachOrStart(new SpeechTranscript("Hilfe", 94), Anchor.AddMilliseconds(2400), inventory, window);

        Assert.IsType<WakeStitchAttachedResult>(attached);
    }

    [Fact]
    public void AttachOrStart_ExpiredCarryWithIneligibleCurrentCycle_StillSurfacesExpired()
    {
        var stitcher = new WakeUtteranceStitcher();
        var inventory = Inventory("okay hilfe");
        var window = Window(maxPauseMs: 850);

        stitcher.AttachOrStart(new SpeechTranscript("Okay", 80), Anchor, inventory, window);

        var expired = stitcher.AttachOrStart(new SpeechTranscript("Wetter", 70), Anchor.AddMilliseconds(2000), inventory, window);

        Assert.IsType<WakeStitchExpiredResult>(expired);
        Assert.Null(stitcher.CarriedTranscriptText);
    }
}
