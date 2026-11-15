using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Zählt Fehlversuche für das aktuelle Feld und fordert beim modellierten
/// Grenzwert die Feldhilfe an. Danach beginnt der Zähler erneut. Auch ein
/// Feldwechsel setzt ihn zurück. <see cref="IsHelpDueOnNextFailure"/> ermöglicht
/// eine passende Ankündigung, bevor die nächste Registrierung die Hilfe auslöst.
/// </summary>
internal sealed class FormVoiceCommandAutoHelpTracker
{
    private readonly int _maxFailuresBeforeHelp;
    private readonly IFormVoiceCommandPromptService _promptService;
    private readonly IFormVoiceCommandDialogStateTracker _stateTracker;
    private readonly IFormDialogRuntime _interpreter;
    private readonly string _isoCode;
    private readonly VoiceDialogSettings _voiceSettings;

    private string? _fieldId;
    private int _failureCount;

    public FormVoiceCommandAutoHelpTracker(
        VoiceDialogSettings voiceSettings,
        IFormVoiceCommandPromptService promptService,
        IFormVoiceCommandDialogStateTracker stateTracker,
        IFormDialogRuntime interpreter,
        string isoCode)
    {
        ArgumentNullException.ThrowIfNull(voiceSettings);
        ArgumentNullException.ThrowIfNull(promptService);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        _maxFailuresBeforeHelp = Math.Max(0, voiceSettings.GetMaxRetries());
        _promptService = promptService;
        _stateTracker = stateTracker;
        _interpreter = interpreter;
        _isoCode = isoCode;
        _voiceSettings = voiceSettings;
    }

    public void Reset()
    {
        _fieldId = null;
        _failureCount = 0;
    }

    /// <summary>
    /// Gibt an, ob <see cref="RegisterFailureAsync"/> beim nächsten Aufruf für
    /// dieses Feld die Hilfe anfordert. Bei MaxRetries gleich null bleibt
    /// die automatische Hilfe ausgeschaltet.
    /// </summary>
    public bool IsHelpDueOnNextFailure(string fieldId)
    {
        if (_maxFailuresBeforeHelp <= 0)
        {
            return false;
        }

        var failureCount = string.Equals(_fieldId, fieldId, StringComparison.Ordinal) ? _failureCount : 0;
        return failureCount + 1 >= _maxFailuresBeforeHelp;
    }

    public async Task RegisterFailureAsync(string fieldId, CancellationToken cancellationToken)
    {
        if (_maxFailuresBeforeHelp <= 0)
        {
            return;
        }

        if (!string.Equals(_fieldId, fieldId, StringComparison.Ordinal))
        {
            _fieldId = fieldId;
            _failureCount = 0;
        }

        _failureCount++;

        if (_failureCount < _maxFailuresBeforeHelp)
        {
            return;
        }

        _failureCount = 0;
        await _promptService
            .SpeakHelpForFocusedFieldAsync(_interpreter, _stateTracker, _isoCode, _voiceSettings, cancellationToken)
            .ConfigureAwait(false);
    }
}


