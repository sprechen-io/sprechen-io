namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class PcmSpeechCaptureStartContextPolicyTests
{
    [Fact]
    public void CleanListening_DisablesPlaybackCalibrationAndLeakageGuard()
    {
        var policy = CreatePolicy(SpeechCaptureStartContext.CleanListening);

        Assert.False(policy.UsesPlaybackLeakageStartGuard);
        Assert.False(policy.UsesAdaptiveSpeechFloor());
        Assert.False(policy.AcceptsStartupCalibrationSample(averageAmplitude: 0));
        Assert.Equal(0, policy.RecentPlaybackSpeechStartGuardMs);
        Assert.Equal(0, policy.GetCalibrationChunkCount());
        Assert.False(policy.ShouldDropBufferedPreRollOnSpeechStart());
        // Ohne eigene Wiedergabe wird keine adaptive Obergrenze benötigt; die Begrenzung bleibt wirkungslos.
        Assert.Null(policy.ResolveIdleAdaptiveFloorCeiling());
    }

    [Fact]
    public void AssistantPlaybackActive_UsesActiveCalibrationAndDropsPreRoll()
    {
        var policy = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackActive);

        Assert.True(policy.UsesPlaybackLeakageStartGuard);
        Assert.True(policy.UsesAdaptiveSpeechFloor());
        Assert.True(policy.AcceptsStartupCalibrationSample(averageAmplitude: 2000));
        Assert.Equal(8, policy.GetCalibrationChunkCount());
        Assert.Equal(220, policy.ResolveSpeechStartAdaptiveMargin());
        Assert.True(policy.ShouldDropBufferedPreRollOnSpeechStart());
        Assert.Equal(0, policy.RecentPlaybackSpeechStartGuardMs);
        // Während eigener Wiedergabe darf die Leerlaufbegrenzung den gesonderten Abstand von 220 nicht verändern.
        Assert.Null(policy.ResolveIdleAdaptiveFloorCeiling());
    }

    [Fact]
    public void RecentlySettledIdle_UsesRecentPlaybackGuardAndLowAmplitudeCalibration()
    {
        var policy = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle);

        Assert.True(policy.UsesPlaybackLeakageStartGuard);
        Assert.True(policy.UsesAdaptiveSpeechFloor());
        Assert.True(policy.AcceptsStartupCalibrationSample(averageAmplitude: 999));
        Assert.False(policy.AcceptsStartupCalibrationSample(averageAmplitude: 1000));
        Assert.Equal(2, policy.GetCalibrationChunkCount());
        Assert.Equal(120, policy.ResolveSpeechStartAdaptiveMargin());
        Assert.Equal(160, policy.RecentPlaybackSpeechStartGuardMs);
        Assert.False(policy.ShouldDropBufferedPreRollOnSpeechStart());
        // Nur der Leerlauf nach Wiedergabe begrenzt die adaptive Schwelle.
        // Die Obergrenze 350 + 300 bleibt über der statischen Mindestschwelle.
        Assert.Equal(650, policy.ResolveIdleAdaptiveFloorCeiling());
        Assert.True(policy.ResolveIdleAdaptiveFloorCeiling() > 350);
    }

    [Fact]
    public void RecentlySettledContinuation_UsesRecentPlaybackGuardWithoutCalibration()
    {
        var policy = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation);

        Assert.True(policy.UsesPlaybackLeakageStartGuard);
        Assert.False(policy.UsesAdaptiveSpeechFloor());
        Assert.False(policy.AcceptsStartupCalibrationSample(averageAmplitude: 0));
        Assert.Equal(0, policy.GetCalibrationChunkCount());
        Assert.Equal(160, policy.RecentPlaybackSpeechStartGuardMs);
        Assert.False(policy.ShouldDropBufferedPreRollOnSpeechStart());
        // Bei der Fortsetzung nach abgeklungener Wiedergabe ist keine adaptive Begrenzung vorgesehen.
        Assert.Null(policy.ResolveIdleAdaptiveFloorCeiling());
    }

    [Fact]
    public void RecentlySettledIdleGuard_WidensToCoverLargeOutputLatencyTail()
    {
        // Die gemeldeten 300 ms Ausgangslatenz plus 30 ms Reserve übersteigen die Grundfrist von 160 ms.
        var policy = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, outputLatencyMs: 300);
        Assert.Equal(330, policy.RecentPlaybackSpeechStartGuardMs);
    }

    [Fact]
    public void RecentlySettledContinuationGuard_WidensToCoverLargeOutputLatencyTail()
    {
        var policy = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation, outputLatencyMs: 300);
        Assert.Equal(330, policy.RecentPlaybackSpeechStartGuardMs);
    }

    [Fact]
    public void RecentPlaybackGuard_LeavesGuardUnchangedForSmallInteractiveLatency()
    {
        // 20 ms Latenz plus Reserve liegen unter 160 ms; die konfigurierte Grundfrist darf nicht verkürzt werden.
        var idle = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle, outputLatencyMs: 20);
        var continuation = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation, outputLatencyMs: 20);

        Assert.Equal(160, idle.RecentPlaybackSpeechStartGuardMs);
        Assert.Equal(160, continuation.RecentPlaybackSpeechStartGuardMs);
    }

    [Fact]
    public void RelaxedShortConfirmationGuard_GrowsOnlyModestlyAtInteractiveLatency()
    {
        // Die Grundfrist von 60 ms für Kurzbestätigungen wächst bei 40 ms Latenz auf 70 ms,
        // bei 300 ms Latenz auf 330 ms. Geprüft wird die Frist, keine tatsächliche Spracherkennung.
        var interactive = new PcmSpeechCaptureStartContextPolicy(
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            RelaxedShortConfirmationOptions(),
            recentPlaybackOutputLatencyMs: 40);
        var playback = new PcmSpeechCaptureStartContextPolicy(
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            RelaxedShortConfirmationOptions(),
            recentPlaybackOutputLatencyMs: 300);

        Assert.Equal(70, interactive.RecentPlaybackSpeechStartGuardMs);
        Assert.Equal(330, playback.RecentPlaybackSpeechStartGuardMs);
    }

    [Fact]
    public void OutputLatency_DoesNotOpenGuardForNonRecentPlaybackContexts()
    {
        // Ohne kürzlich beendete Wiedergabe bleibt diese Schutzfrist auch bei hoher Latenz unbenutzt.
        var clean = CreatePolicy(SpeechCaptureStartContext.CleanListening, outputLatencyMs: 500);
        var active = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackActive, outputLatencyMs: 500);

        Assert.Equal(0, clean.RecentPlaybackSpeechStartGuardMs);
        Assert.Equal(0, active.RecentPlaybackSpeechStartGuardMs);
    }

    [Fact]
    public void IsBelowPlaybackLeakageBypassThreshold_UsesConfiguredExclusiveThreshold()
    {
        var policy = CreatePolicy(SpeechCaptureStartContext.AssistantPlaybackActive);

        Assert.True(policy.IsBelowPlaybackLeakageBypassThreshold(999));
        Assert.False(policy.IsBelowPlaybackLeakageBypassThreshold(1000));
    }

    [Fact]
    public void IdleAdaptiveFloorCeiling_WithZeroMargin_NeverDropsBelowStaticThreshold()
    {
        // Auch bei Abstand null darf die adaptive Obergrenze nicht unter die statische Sprachbeginnschwelle fallen.
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechAmplitudeThreshold = 350,
            IdleAdaptiveFloorMaxMargin = 0,
        };
        var policy = new PcmSpeechCaptureStartContextPolicy(
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            options);

        Assert.Equal(350, policy.ResolveIdleAdaptiveFloorCeiling());
    }

    private static PcmSpeechCaptureSegmenterOptions RelaxedShortConfirmationOptions()
    {
        return new PcmSpeechCaptureSegmenterOptions
        {
            RecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(60),
            OutputLatencyGuardMargin = TimeSpan.FromMilliseconds(30),
        };
    }

    private static PcmSpeechCaptureStartContextPolicy CreatePolicy(
        SpeechCaptureStartContext context,
        double outputLatencyMs = 0)
    {
        return new PcmSpeechCaptureStartContextPolicy(
            context,
            new PcmSpeechCaptureSegmenterOptions
            {
                ActivePlaybackCalibrationChunkCount = 8,
                RecentPlaybackCalibrationChunkCount = 2,
                PlaybackLeakageBypassAmplitudeThreshold = 1000,
                SpeechStartAdaptiveMargin = 120,
                ActivePlaybackSpeechStartAdaptiveMargin = 220,
                RecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(160),
                OutputLatencyGuardMargin = TimeSpan.FromMilliseconds(30),
            },
            outputLatencyMs);
    }
}
