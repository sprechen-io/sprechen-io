namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet die positive Revision eines Kandidatenbestands, damit Übergänge veraltete
/// Auswahlkontexte unterscheiden können.
/// </summary>
public sealed record SelectionRevision
{
    /// <summary>
    /// Erzeugt eine Revision aus einem positiven Wert. Ein monotoner Fortschritt wird hier ohne
    /// vorherige Revision nicht geprüft.
    /// </summary>
    /// <param name="value">Der positive Revisionswert.</param>
    public SelectionRevision(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A selection revision must be positive.");
        }

        Value = value;
    }

    /// <summary>
    /// Stellt die erste Revision des Ausgangsbestands bereit.
    /// </summary>
    public static SelectionRevision Initial { get; } = new(1);

    /// <summary>
    /// Stellt den positiven Revisionswert bereit.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Erzeugt den unmittelbaren Nachfolger und löst bei ausgeschöpftem Zahlenbereich eine Ausnahme
    /// aus.
    /// </summary>
    public SelectionRevision Next()
    {
        return new SelectionRevision(checked(Value + 1));
    }
}
