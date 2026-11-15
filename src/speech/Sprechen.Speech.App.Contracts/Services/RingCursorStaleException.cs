namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Zeigt an, dass ein <see cref="SampleCursor"/> vor dem ältesten noch gespeicherten
/// Sample des Rings liegt und die angeforderten Daten bereits überschrieben wurden.
/// </summary>
/// <remarks>
/// Der Ring meldet den Datenverlust ausdrücklich, statt den Cursor still auf die
/// nächste verfügbare Position zu verschieben. Verbraucher müssen innerhalb des
/// verfügbaren Rückblicks lesen.
/// </remarks>
public sealed class RingCursorStaleException : InvalidOperationException
{
    /// <summary>Stabiler Anwendungscode für Telemetrie und Tests.</summary>
    public const string CodeValue = "RING_CURSOR_STALE";

    public RingCursorStaleException(long requestedSampleIndex, long earliestRetainedSampleIndex)
        : base(
            $"Ring cursor at sample {requestedSampleIndex} fell behind the earliest retained " +
            $"sample {earliestRetainedSampleIndex}; the writer has wrapped past the cursor.")
    {
        Code = CodeValue;
        RequestedSampleIndex = requestedSampleIndex;
        EarliestRetainedSampleIndex = earliestRetainedSampleIndex;
    }

    public string Code { get; }

    public long RequestedSampleIndex { get; }

    public long EarliestRetainedSampleIndex { get; }
}
