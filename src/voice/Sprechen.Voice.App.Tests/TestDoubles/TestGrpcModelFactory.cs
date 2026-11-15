using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal static class TestGrpcModelFactory
{
    public static AppSpeechTranscript CreateTranscript(string text, int? confidencePercent = null)
    {
        return new AppSpeechTranscript(text, confidencePercent);
    }
}
