using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class SwitchLanguageThenEchoVoiceCommandDialog(
    ISpeechInputClient speechInputClient,
    ITextToSpeechPlayer textToSpeechPlayer,
    string targetIsoCode,
    ImmutableDictionary<string, string> preservedValuesByVariableId) : IVoiceCommandDialog
{
    private readonly ISpeechInputClient _speechInputClient = speechInputClient ?? throw new ArgumentNullException(nameof(speechInputClient));
    private readonly ITextToSpeechPlayer _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
    private readonly string _targetIsoCode = IsoCode.TryNormalizeIso6391(targetIsoCode)
        ?? throw new ArgumentException("Expected ISO 639-1 language code.", nameof(targetIsoCode));
    private readonly ImmutableDictionary<string, string> _preservedValuesByVariableId = preservedValuesByVariableId
        ?? throw new ArgumentNullException(nameof(preservedValuesByVariableId));
    private readonly List<(string LanguageCode, ImmutableDictionary<string, string> InitialValuesByVariableId)> _executions = [];
    private int _executionCount;

    public IReadOnlyList<(string LanguageCode, ImmutableDictionary<string, string> InitialValuesByVariableId)> Executions => _executions;

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

        var normalizedLanguageCode = IsoCode.TryNormalizeIso6391(languageCode)
            ?? throw new InvalidOperationException("Expected normalized ISO 639-1 language code.");
        var valuesSnapshot = initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);

        _executions.Add((normalizedLanguageCode, valuesSnapshot));

        if (_executionCount++ == 0)
        {
            return VoiceCommandDialogResult.ChangeLanguage(
                applicationId,
                formId,
                _targetIsoCode,
                _preservedValuesByVariableId);
        }

        var utterance = await _speechInputClient.RecordAsync(cancellationToken).ConfigureAwait(false);
        var transcript = await _speechInputClient.SpeechToTextAsync(utterance, normalizedLanguageCode, RecognitionProfile.Conversational, cancellationToken).ConfigureAwait(false);
        var text = transcript.Text?.Trim() ?? string.Empty;
        await _textToSpeechPlayer.SpeakAsync(text, normalizedLanguageCode, cancellationToken).ConfigureAwait(false);

        return VoiceCommandDialogResult.Abandoned(applicationId, formId, valuesSnapshot);
    }
}
