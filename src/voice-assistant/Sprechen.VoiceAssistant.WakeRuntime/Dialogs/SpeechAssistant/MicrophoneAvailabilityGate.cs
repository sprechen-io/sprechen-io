namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidet anhand aufeinanderfolgender Erfassungsfehler und bestätigter
/// Ansagen, wann ein Mikrofonhinweis freigegeben wird. Der Mindestabstand
/// verdoppelt sich bis zur Obergrenze. Reguläre Erfassungen setzen die Fehlerfolge
/// zurück. Eine ausstehende Entwarnung bleibt bis zur Bestätigung erhalten.
/// Die Klasse führt selbst keine Wiedergabe aus.
/// </summary>
internal sealed class MicrophoneAvailabilityGate
{
    private readonly ISystemClock _clock;
    private readonly object _sync = new();

    private int _consecutiveUnavailableStreak;
    private int _announcementSpokenCount;
    private DateTimeOffset? _lastAnnouncementAt;
    private bool _announcedOutageAwaitingRecovery;

    public MicrophoneAvailabilityGate(ISystemClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Beginnt einen neuen Zuhörlauf, auch nach Pause und Fortsetzung.
    /// Entfernt den bisherigen Zeitanker, damit er keine Ansage im neuen Lauf
    /// verzögert. Eskalationszähler und ausstehende Entwarnung bleiben erhalten.
    /// </summary>
    public void BeginListeningRun()
    {
        lock (_sync)
        {
            _lastAnnouncementAt = null;
        }
    }

    public void RecordUnavailable()
    {
        lock (_sync)
        {
            _consecutiveUnavailableStreak++;
        }
    }

    /// <summary>
    /// Setzt die Fehlerfolge nach regulärer Erfassung zurück und meldet, ob noch
    /// eine Entwarnung aussteht. Erst <see cref="RecordRecoveryAnnouncementSpoken"/>
    /// bestätigt deren erfolgreiche Wiedergabe.
    /// </summary>
    public bool RecordHealthyCapture()
    {
        lock (_sync)
        {
            _consecutiveUnavailableStreak = 0;
            if (!_announcedOutageAwaitingRecovery)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Bestätigt die erfolgreiche Entwarnung und setzt die Ansagefolge für einen neuen Ausfall zurück.
    /// </summary>
    public void RecordRecoveryAnnouncementSpoken()
    {
        lock (_sync)
        {
            _announcedOutageAwaitingRecovery = false;
            _announcementSpokenCount = 0;
            _lastAnnouncementAt = null;
        }
    }

    /// <summary>
    /// Vermerkt eine erfolgreiche Aktivierung. Eine ausstehende Entwarnung bleibt
    /// bis zu ihrer Bestätigung erhalten. Ohne offenen Ausfall wird die
    /// Ansagefolge für einen späteren unabhängigen Vorfall zurückgesetzt.
    /// </summary>
    public void RecordWake()
    {
        lock (_sync)
        {
            _consecutiveUnavailableStreak = 0;
            if (_announcedOutageAwaitingRecovery)
            {
                return;
            }

            _announcementSpokenCount = 0;
            _lastAnnouncementAt = null;
        }
    }

    /// <summary>
    /// Prüft ohne Zustandsänderung, ob Fehlerschwelle und Mindestabstand
    /// eine Ansage freigeben. Die Anzahl der Ansagen ist unbegrenzt.
    /// Nur der wachsende Abstand erreicht eine Obergrenze.
    /// </summary>
    public bool ShouldSpeak(MicrophoneAvailabilityGuidance guidance)
    {
        ArgumentNullException.ThrowIfNull(guidance);

        lock (_sync)
        {
            if (_consecutiveUnavailableStreak < guidance.UnavailableThreshold)
            {
                return false;
            }

            var effectiveCooldown = guidance.Backoff.IntervalAfter(_announcementSpokenCount);
            if (_lastAnnouncementAt is { } lastAt && _clock.UtcNow - lastAt < effectiveCooldown)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Vermerkt eine bestätigte Ausfallansage: setzt den Zeitanker, erhöht
    /// den Zähler für den nächsten Mindestabstand und setzt die Fehlerfolge zurück.
    /// </summary>
    public void RecordAnnouncementSpoken()
    {
        lock (_sync)
        {
            _lastAnnouncementAt = _clock.UtcNow;
            _announcementSpokenCount++;
            _consecutiveUnavailableStreak = 0;
            _announcedOutageAwaitingRecovery = true;
        }
    }
}
