namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält Dauer und mittleren Absolutpegel eines PCM-Blocks zusammen,
/// damit die Segmentierung Zeitfortschritt und Sprachschwelle aus derselben Analyse erhält.
/// </summary>
internal readonly record struct PcmAudioChunkAnalysis(
    double DurationMs,
    int AverageAmplitude);
