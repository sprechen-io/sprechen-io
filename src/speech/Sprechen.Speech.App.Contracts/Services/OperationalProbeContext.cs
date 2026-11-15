namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Kennung und Bedingung, die der Host für einen betrieblichen Voice-Testlauf ausstellt.</summary>
public sealed record OperationalProbeContext
{
    public OperationalProbeContext(string probeId, string condition)
    {
        if (probeId is null || probeId.Length != 38
            || !probeId.StartsWith("probe-", StringComparison.Ordinal)
            || !probeId.AsSpan(6).ToArray().All(static value => value is >= '0' and <= '9' or >= 'a' and <= 'f')
            || condition != "B")
        {
            throw new ArgumentException("The operational probe context is invalid.");
        }

        ProbeId = probeId;
        Condition = condition;
    }

    public string ProbeId { get; }

    public string Condition { get; }
}
