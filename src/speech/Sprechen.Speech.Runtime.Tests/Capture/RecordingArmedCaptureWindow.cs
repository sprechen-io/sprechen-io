namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Protokolliert das Lesen der Fensterbeobachtungen und die Entsorgung, damit
/// der Test deren Reihenfolge prüfen kann.
/// </summary>
internal sealed class RecordingArmedCaptureWindow : IArmedCaptureWindow
{
    private readonly List<string> _calls = [];

    internal IReadOnlyList<string> Calls => _calls;

    public int MaxAmplitudeObserved => Record(nameof(MaxAmplitudeObserved), 1);

    public int ObservedFrameCount => Record(nameof(ObservedFrameCount), 2);

    public int EstimatedFloor => Record(nameof(EstimatedFloor), 3);

    public int AppliedOnsetThreshold => Record(nameof(AppliedOnsetThreshold), 4);

    public void Dispose() => _calls.Add(nameof(Dispose));

    private int Record(string call, int value)
    {
        _calls.Add(call);
        return value;
    }
}
