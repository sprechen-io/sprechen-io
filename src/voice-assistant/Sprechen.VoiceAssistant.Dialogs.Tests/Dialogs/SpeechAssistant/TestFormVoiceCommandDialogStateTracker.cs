namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal sealed class TestFormVoiceCommandDialogStateTracker : IFormVoiceCommandDialogStateTracker
{
    private readonly List<string>? _wireStates;

    public TestFormVoiceCommandDialogStateTracker(List<string>? wireStates = null)
    {
        _wireStates = wireStates;
    }

    public bool WasSubmitted { get; private set; }

    public bool WasCancelled { get; private set; }

    public void BeginErrorPrompt()
    {
        _wireStates?.Add(DialogStates.ErrorPrompt);
    }

    public void BeginCancelConfirmation()
    {
        _wireStates?.Add(DialogStates.CancelConfirmation);
    }

    public void RecordSubmitted()
    {
        WasSubmitted = true;
    }

    public void RecordCancelled()
    {
        WasCancelled = true;
    }

    private readonly HashSet<string> _taughtTeachingTopics = new(StringComparer.Ordinal);

    /// <summary>
    /// Die bisher als vermittelt markierten Themen ohne doppelte Kennungen.
    /// </summary>
    public IReadOnlyCollection<string> TaughtTeachingTopics => _taughtTeachingTopics;

    public bool HasAlreadyTaught(string teachingTopicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teachingTopicId);
        return !_taughtTeachingTopics.Add(teachingTopicId);
    }
}
