using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class EchoVoiceCommandDialog(
    ISpeechInputClient speechInputClient,
    ITextToSpeechPlayer textToSpeechPlayer) : IVoiceCommandDialog
{
    private readonly ISpeechInputClient _speechInputClient = speechInputClient ?? throw new ArgumentNullException(nameof(speechInputClient));
    private readonly ITextToSpeechPlayer _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));

    public async Task<VoiceCommandDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return VoiceCommandDialogResult.Abandoned(
                applicationId,
                formId,
                initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal));
        }

        CapturedSpeechAudio utterance;
        try
        {
            utterance = await _speechInputClient.RecordAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (NoSpeechDetectedException)
        {
            return VoiceCommandDialogResult.Abandoned(
                applicationId,
                formId,
                initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal));
        }

        var transcript = await _speechInputClient.SpeechToTextAsync(utterance, languageCode, RecognitionProfile.Conversational, cancellationToken).ConfigureAwait(false);
        var text = transcript.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return VoiceCommandDialogResult.Abandoned(
                applicationId,
                formId,
                initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal));
        }

        await _textToSpeechPlayer.SpeakAsync(text, languageCode, cancellationToken).ConfigureAwait(false);

        return VoiceCommandDialogResult.Abandoned(
            applicationId,
            formId,
            initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal));
    }
}


