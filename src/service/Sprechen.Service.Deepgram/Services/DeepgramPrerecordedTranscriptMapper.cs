using Deepgram.Models.Listen.v1.REST;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Übernimmt Text und Erkennungssicherheit der ersten Alternative des ersten Antwortkanals.
/// Fehlt ein Transkript, wird ein leerer Text zurückgegeben.
/// </summary>
internal sealed class DeepgramPrerecordedTranscriptMapper
{
    internal SpeechTranscript Map(SyncResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var top = response.Results?.Channels?.FirstOrDefault()?.Alternatives?.FirstOrDefault();
        var text = top?.Transcript ?? string.Empty;
        var confidencePercent = SttConfidencePercent.FromNormalized(top?.Confidence);

        return new SpeechTranscript(text, confidencePercent);
    }
}
