using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

/// <summary>
/// Erfasst im ersten Testaufruf einen Feldwert und liefert ihn bei VoiceInputPausedException als pausiertes Ergebnis zurück.
/// Ab dem zweiten Aufruf gibt das Testdouble die übergebenen Anfangswerte als abgesendet zurück.
/// Damit lässt sich prüfen, ob der Assistent den Wert über die Pause erhält.
/// </summary>
internal sealed class PauseThenSubmitVoiceCommandDialog(
    ISpeechInputClient speechInputClient,
    string fieldVariableId) : IVoiceCommandDialog
{
    private readonly ISpeechInputClient _speechInputClient =
        speechInputClient ?? throw new ArgumentNullException(nameof(speechInputClient));

    private readonly string _fieldVariableId = string.IsNullOrWhiteSpace(fieldVariableId)
        ? throw new ArgumentException("Field variable id is required.", nameof(fieldVariableId))
        : fieldVariableId.Trim();

    private readonly object _gate = new();
    private readonly List<(string ApplicationId, string? FormId, ImmutableDictionary<string, string> InitialValuesByVariableId)> _executions = [];
    private int _executionCount;
    private volatile bool _firstFieldDictated;

    public bool FirstFieldDictated => _firstFieldDictated;

    public IReadOnlyList<(string ApplicationId, string? FormId, ImmutableDictionary<string, string> InitialValuesByVariableId)> Executions
    {
        get
        {
            lock (_gate)
            {
                return _executions.ToArray();
            }
        }
    }

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

        var initialValues = initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);
        lock (_gate)
        {
            _executions.Add((applicationId, formId, initialValues));
        }

        if (_executionCount++ != 0)
        {
            return VoiceCommandDialogResult.Submitted(applicationId, RequireFormId(formId), initialValues);
        }

        var utterance = await _speechInputClient.RecordAsync(cancellationToken).ConfigureAwait(false);
        var transcript = await _speechInputClient.SpeechToTextAsync(utterance, languageCode, RecognitionProfile.Conversational, cancellationToken).ConfigureAwait(false);
        var dictatedValue = transcript.Text?.Trim() ?? string.Empty;

        _firstFieldDictated = true;

        try
        {
            _ = await _speechInputClient.RecordAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (VoiceInputPausedException)
        {
            return VoiceCommandDialogResult.Paused(
                applicationId,
                RequireFormId(formId),
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)
                    .Add(_fieldVariableId, dictatedValue));
        }

        return VoiceCommandDialogResult.Abandoned(applicationId, formId, initialValues);
    }

    private static string RequireFormId(string? formId)
        => string.IsNullOrWhiteSpace(formId)
            ? throw new InvalidOperationException("Voice command dialog expected a selected form id.")
            : formId.Trim();
}
