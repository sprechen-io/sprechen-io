namespace Sprechen.Service.Abstractions;

/// <summary>
/// Erkennungssicherheit auf der normalisierten Skala [0,1] mit ganzzahliger Prozentdarstellung.
/// Initialisierte Werte entstehen über <see cref="FromNormalized"/> oder <see cref="FromPercent"/>.
/// Ein uninitialisierter Wert lässt sich nicht auslesen. Fehlende Angaben können nullable dargestellt werden.
/// </summary>
public readonly record struct RecognitionConfidence
{
    private readonly double? _normalized;

    private RecognitionConfidence(double normalized) => _normalized = normalized;

    /// <summary>
    /// Begrenzt einen endlichen Wert auf [0,1]. NaN und unendliche Werte werden abgewiesen.
    /// </summary>
    public static RecognitionConfidence FromNormalized(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentException("Recognition confidence must be a finite number.", nameof(value));
        }

        return new RecognitionConfidence(Math.Clamp(value, 0d, 1d));
    }

    /// <summary>
    /// Übernimmt eine ganzzahlige Prozentangabe von 0 bis 100. Die Intervallmitte sichert die
    /// Rückgabe desselben Werts durch <see cref="AsPercent"/> trotz Gleitkommaabrundung.
    /// </summary>
    public static RecognitionConfidence FromPercent(int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Recognition confidence percent must be in [0, 100].");
        }

        return new RecognitionConfidence(Math.Clamp((percent + 0.5d) / 100d, 0d, 1d));
    }

    /// <summary>Die kanonische normalisierte Sicherheit in [0,1]. Wirft, wenn der Wert uninitialisiert ist.</summary>
    public double AsNormalized => _normalized ?? throw new InvalidOperationException("RecognitionConfidence is uninitialized.");

    /// <summary>
    /// Ganzzahliger Prozentwert: normalisierte Erkennungssicherheit mal 100, anschließend abgerundet.
    /// </summary>
    public int AsPercent => (int)Math.Floor(AsNormalized * 100d);

    public override string ToString() =>
        _normalized is { } normalized ? $"{(int)Math.Floor(normalized * 100d)}%" : "RecognitionConfidence(uninitialized)";
}
