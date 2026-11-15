namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidet vor der ersten erfolgreichen Aktivierung über Erkennungshinweise.
/// Fehlerschwelle und wachsender Mindestabstand begrenzen wiederholte Hinweise,
/// weil auch ungerichtete Geräusche verarbeitet werden. Die Anzahl ist nicht
/// begrenzt. Nach einer erfolgreichen Aktivierung bleiben weitere Hinweise
/// für die Lebensdauer dieser Instanz gesperrt. Die Klasse spielt nichts ab.
/// </summary>
internal sealed class WakeRecoveryGuidanceGate
{
    private readonly ISystemClock _clock;
    private readonly object _sync = new();

    private int _consecutiveMissStreak;
    private int _guidanceSpokenCount;
    private DateTimeOffset? _lastGuidanceAt;
    private bool _hasWokenThisSession;

    public WakeRecoveryGuidanceGate(ISystemClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Vermerkt eine nicht erkannte Aktivierung, solange noch keine erfolgreich war.
    /// Der Aufrufer muss eine laufende Segmentzusammenführung vorher ausschließen,
    /// damit unvollständige Äußerungen die Fehlerfolge nicht erhöhen.
    /// </summary>
    public void RecordMiss()
    {
        lock (_sync)
        {
            if (_hasWokenThisSession)
            {
                return;
            }

            _consecutiveMissStreak++;
        }
    }

    /// <summary>
    /// Sperrt nach erfolgreicher Aktivierung weitere Erkennungshinweise für diese
    /// Instanz.
    /// </summary>
    public void RecordWake()
    {
        lock (_sync)
        {
            _hasWokenThisSession = true;
            _consecutiveMissStreak = 0;
        }
    }

    /// <summary>
    /// Prüft ohne Zustandsänderung, ob erste Aktivierung, Fehlerfolge und
    /// Mindestabstand einen Hinweis zulassen. Nach der Entscheidung zum Versuch
    /// aktualisiert RecordGuidanceSpoken Zeitanker und Zähler. Eine Wiedergabe
    /// wird durch diese Prüfung nicht bestätigt.
    /// </summary>
    public bool ShouldSpeakGuidance(WakeRecoveryGuidance guidance)
    {
        ArgumentNullException.ThrowIfNull(guidance);

        lock (_sync)
        {
            if (_hasWokenThisSession)
            {
                return false;
            }

            if (_consecutiveMissStreak < guidance.MissThreshold)
            {
                return false;
            }

            var effectiveCooldown = guidance.Backoff.IntervalAfter(_guidanceSpokenCount);
            if (_lastGuidanceAt is { } lastGuidanceAt && _clock.UtcNow - lastGuidanceAt < effectiveCooldown)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Wählt vor <see cref="RecordGuidanceSpoken"/> den direkten Hinweis für den
    /// ersten Versuch, danach stets denselben aufgelösten Text mit Alternativen.
    /// </summary>
    public string SelectGuidanceText(WakeRecoveryGuidance guidance)
    {
        ArgumentNullException.ThrowIfNull(guidance);

        lock (_sync)
        {
            return _guidanceSpokenCount == 0
                ? guidance.PrimaryGuidanceText
                : guidance.AlternativesGuidanceText;
        }
    }

    /// <summary>
    /// Vermerkt einen Hilfeversuch vor dessen Wiedergabe: setzt den Zeitanker,
    /// erhöht den Zähler für Mindestabstand und Textauswahl und setzt die
    /// Fehlerfolge zurück.
    /// </summary>
    public void RecordGuidanceSpoken()
    {
        lock (_sync)
        {
            _lastGuidanceAt = _clock.UtcNow;
            _guidanceSpokenCount++;
            _consecutiveMissStreak = 0;
        }
    }
}
