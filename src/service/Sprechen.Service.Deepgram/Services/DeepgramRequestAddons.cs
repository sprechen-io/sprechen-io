namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erzeugt den Anfragezusatz mip_opt_out=true zur Abwahl der Modellverbesserung beim Anbieter.
/// </summary>
internal static class DeepgramRequestAddons
{
    private const string ModelImprovementOptOutKey = "mip_opt_out";
    private const string ModelImprovementOptOutValue = "true";

    public static Dictionary<string, string> CreateModelImprovementOptOut() =>
        new(StringComparer.Ordinal)
        {
            [ModelImprovementOptOutKey] = ModelImprovementOptOutValue
        };
}

