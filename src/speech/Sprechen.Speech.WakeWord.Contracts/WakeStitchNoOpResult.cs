namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Kennzeichnet einen Zyklus ohne neu begonnene oder erfolgreiche Verknüpfung.
/// Eine bereits vorgemerkte Teiläußerung kann dabei erhalten bleiben.
/// </summary>
public sealed class WakeStitchNoOpResult : WakeStitchResult
{
    public static readonly WakeStitchNoOpResult Instance = new();

    private WakeStitchNoOpResult()
    {
    }

    /// <inheritdoc />
    public override TResult Accept<TResult>(IWakeStitchResultVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.VisitNoOp(this);
    }
}
