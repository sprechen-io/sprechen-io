using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Vereinigt die modellierten Negationshinweise aller unterstützten Sprachen
/// aus dem Sprachlexikon, sodass sie unabhängig von der Feldsprache verfügbar sind.
/// </summary>
/// <remarks>
/// Die Vereinigung ist nicht auf Feld- oder Sitzungssprache eingeschränkt. Dadurch
/// können auch kurze fremdsprachige Hinweise eine Auswahl in einer anderen Sprache
/// verhindern. Eine Einschränkung auf einzelne Sprachen müsste ausdrücklich
/// im Aufrufvertrag und dessen Verwendung abgebildet werden.
/// </remarks>
public sealed class ModeledNegationParticleResolver : INegationParticleResolver
{
    public IReadOnlyCollection<string> ResolveNegationParticles()
    {
        var particles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            foreach (var cue in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNegationCues(isoCode))
            {
                particles.Add(cue);
            }
        }

        return particles;
    }
}
