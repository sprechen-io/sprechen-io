namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt den wachsenden Mindestabstand zwischen wiederholten Hinweisen.
/// Nach null oder einer vermerkten Ansage gilt das Basisintervall. Danach
/// verdoppelt sich der Abstand bis zur Obergrenze. Begrenzt wird der Abstand,
/// nicht die Anzahl der Ansagen.
/// </summary>
internal sealed class EscalatingCooldown
{
    private readonly TimeSpan _baseInterval;
    private readonly TimeSpan _ceiling;

    public EscalatingCooldown(TimeSpan baseInterval, TimeSpan ceiling)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(ceiling, baseInterval);

        _baseInterval = baseInterval;
        _ceiling = ceiling;
    }

    /// <summary>
    /// Liefert den Mindestabstand, der vor der nächsten Ansage einzuhalten ist, nachdem
    /// <paramref name="spokenCount"/> Ansagen vermerkt wurden. Eine negative Anzahl ist keine
    /// Zählung und wird abgewiesen, statt still das Basisintervall zu liefern.
    /// </summary>
    public TimeSpan IntervalAfter(int spokenCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(spokenCount);

        var interval = _baseInterval;
        for (var doubling = 1; doubling < spokenCount; doubling++)
        {
            if (interval >= _ceiling)
            {
                return _ceiling;
            }

            interval += interval;
        }

        return interval > _ceiling ? _ceiling : interval;
    }
}
