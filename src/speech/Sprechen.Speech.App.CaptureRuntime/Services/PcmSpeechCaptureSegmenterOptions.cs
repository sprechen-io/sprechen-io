using System;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Konfiguriert Pegelschwellen, Zeitgrenzen und Audiopuffer des PCM-Segmentierers,
/// damit Sprachbeginn und Abschluss an die Aufnahmesituation angepasst werden können.
/// </summary>
public sealed class PcmSpeechCaptureSegmenterOptions
{
    public int Pcm16BytesPerSample { get; init; } = 2;
    public int PcmSampleRateHz { get; init; } = 16_000;
    public int SpeechAmplitudeThreshold { get; init; } = 350;
    public int SpeechStartAdaptiveMargin { get; init; } = 120;
    public int ActivePlaybackSpeechStartAdaptiveMargin { get; init; } = 220;

    // Begrenzt nur im Leerlauf nach Wiedergabe die adaptive Sprachbeginnschwelle
    // auf SpeechAmplitudeThreshold plus diesen Abstand. So kann ein kurzzeitig hoher
    // Hintergrundpegel die Schwelle nicht darüber hinaus anheben. Der Abstand muss
    // so gewählt werden, dass verbleibende Wiedergabe nicht als Sprache bestätigt wird.
    public int IdleAdaptiveFloorMaxMargin { get; init; } = 300;

    public int PlaybackLeakageBypassAmplitudeThreshold { get; init; } = 1000;
    public int ActivePlaybackCalibrationChunkCount { get; init; } = 8;
    public int RecentPlaybackCalibrationChunkCount { get; init; } = 2;
    // Vor dem Sprachbeginn gepufferte Audioblöcke. Bei 20 ms pro Block ergeben 25 Blöcke 500 ms.
    // Dieser Vorlauf erhält leise Anlaute, die schon vor Überschreiten der Pegelschwelle liegen.
    public int MaxPreRollChunks { get; init; } = 25;
    public int InitialChunkCapacity { get; init; } = 256;
    public TimeSpan MaxInitialSilence { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan RecentPlaybackSpeechStartGuard { get; init; } = TimeSpan.FromMilliseconds(160);

    // Zusätzliche Zeitreserve zur gemessenen Ausgangslatenz. Nach Wiedergabe gilt
    // mindestens RecentPlaybackSpeechStartGuard, bei größerem Bedarf Ausgangslatenz plus Reserve.
    public TimeSpan OutputLatencyGuardMargin { get; init; } = TimeSpan.FromMilliseconds(30);
    public TimeSpan SpeechStartConfirmation { get; init; } = TimeSpan.FromMilliseconds(120);
    public TimeSpan MaxUtteranceDuration { get; init; } = TimeSpan.FromSeconds(15);

    // MaxUtteranceDuration zählt nur Sprachdauer. Diese Grenze zählt die gesamte
    // Audiodauer einschließlich zwischenliegender Stille und verhindert dadurch unbegrenzte Segmente.
    // Muss mindestens MaxUtteranceDuration entsprechen.
    public TimeSpan MaxTotalSegmentDuration { get; init; } = TimeSpan.FromSeconds(30);

    // Behält nach dem letzten Block oberhalb der Sprachschwelle noch Audio,
    // damit leise Auslaute nicht sofort abgeschnitten werden. Die Stillegrenze für den
    // Segmentabschluss bleibt unverändert. Begrenzt wird nur der übernommene Nachlauf.
    public TimeSpan TailSilence { get; init; } = TimeSpan.FromMilliseconds(500);
}

