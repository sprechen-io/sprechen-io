namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Protokolliert Ansage-, Bestätigungs-, TTS- und Hinweistonaufrufe der Testdoubles
/// in SpellingSecretFieldPrivacyEndToEndTests. Die Reihenfolge folgt den Aufrufen,
/// nicht einer Messung hörbarer Wiedergabe. Eine Sperre schützt Schreiben und Lesen.
/// </summary>
internal sealed class FeedbackLog
{
    private readonly object _gate = new();
    private readonly List<string> _ordered = [];
    private readonly List<string> _spoken = [];
    private readonly List<EarconId> _earcons = [];

    public void RecordSpoken(string text)
    {
        lock (_gate)
        {
            _spoken.Add(text);
            _ordered.Add($"TTS:     {text}");
        }
    }

    public void RecordCapturePrompt(string prompt)
    {
        lock (_gate)
        {
            _spoken.Add(prompt);
            _ordered.Add($"CAPTURE: {prompt}");
        }
    }

    public void RecordConfirmPrompt(string prompt)
    {
        lock (_gate)
        {
            _spoken.Add(prompt);
            _ordered.Add($"CONFIRM: {prompt}");
        }
    }

    public void RecordEarcon(EarconId id)
    {
        lock (_gate)
        {
            _earcons.Add(id);
            _ordered.Add($"EARCON:  {id}");
        }
    }

    public IReadOnlyList<string> SpokenLines
    {
        get
        {
            lock (_gate)
            {
                return _spoken.ToArray();
            }
        }
    }

    public IReadOnlyList<EarconId> Earcons
    {
        get
        {
            lock (_gate)
            {
                return _earcons.ToArray();
            }
        }
    }

    public IReadOnlyList<string> Ordered
    {
        get
        {
            lock (_gate)
            {
                return _ordered.ToArray();
            }
        }
    }
}
