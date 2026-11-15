using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt eine gestartete Testsitzung und die aufgezeichneten Sprachausgabeaufrufe für Regressionstests.
/// </summary>
internal sealed record SpeechAssistantApplicationLoadSession(
    SpeechAssistantEndToEndFixture Fixture,
    SpeechAssistantService Assistant,
    RecordingTextToSpeechPlayer TextToSpeechPlayer)
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);

    internal IReadOnlyList<string> SpokenText()
        => [.. TextToSpeechPlayer.Calls.Select(call => call.Text)];

    internal async Task WaitForSpokenAsync(string fragment, string what)
    {
        try
        {
            await Fixture.WaitUntilAsync(
                () => TextToSpeechPlayer.Calls.Any(call =>
                    call.LanguageCode == "de" && call.Text.Contains(fragment, StringComparison.Ordinal)),
                EndToEndTimeout);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException($"Expected {what}. Spoken: {Dump()}", ex);
        }
    }

    private string Dump()
        => string.Join(" | ", TextToSpeechPlayer.Calls.Select(call => $"{call.LanguageCode}:{call.Text}"));
}
