namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält den laufenden PCM-Strom für unabhängig positionierte Leser.
/// Die Sitzungspumpe schreibt Audio-Chunks. Leser adressieren Samples
/// über <see cref="SampleCursor"/>.
/// </summary>
/// <remarks>
/// Die Sitzungspumpe schreibt vor der Weiterleitung an Aufnahmeverbraucher.
/// Diese schreiben die empfangenen Chunks nicht erneut in den Ring. Leser öffnen
/// ihre Cursor über <see cref="OpenCursor"/> und kopieren Daten über <see cref="Read"/>.
/// </remarks>
public abstract class SpeechSampleRing
{
    /// <summary>
    /// Hängt die gelieferten PCM-Samples an den Ring. Als Schreiber ist die Sitzungspumpe vorgesehen.
    /// Der Vertrag verlangt keine Unterstützung mehrerer gleichzeitiger Schreiber.
    /// </summary>
    public abstract void WriteSamples(ReadOnlySpan<byte> samples);

    /// <summary>
    /// Öffnet einen Cursor gemäß <paramref name="origin"/>.
    /// Der zurückgegebene Cursor ist ein Wert im Besitz des Aufrufers.
    /// </summary>
    public abstract SampleCursor OpenCursor(SampleCursorOrigin origin);

    /// <summary>
    /// Kopiert verfügbare Samples ab <paramref name="cursor"/> nach
    /// <paramref name="destination"/> und liefert die Zahl kopierter Bytes, gegebenenfalls 0.
    /// Wirft <see cref="RingCursorStaleException"/>, wenn der Cursor vor dem ältesten
    /// gespeicherten Sample liegt, und <see cref="ArgumentException"/> bei leerem Zielpuffer.
    /// </summary>
    public abstract int Read(SampleCursor cursor, Span<byte> destination);
}
