using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Bündelt die modellierten Lexikonzugänge, damit Ja-Nein-Ausdrücke
/// und sprachabhängige Höflichkeitsformen aus demselben Dialogmodell gelesen werden.
/// </summary>
public sealed class VoiceLexiconModelCatalog
{
    internal VoiceLexiconModelCatalog(VoiceInterface model)
    {
        ArgumentNullException.ThrowIfNull(model);

        YesNo = new VoiceLexiconYesNoCatalog(model);
        Politeness = new VoiceLexiconPolitenessCatalog(model);
    }

    public VoiceLexiconYesNoCatalog YesNo { get; }

    public VoiceLexiconPolitenessCatalog Politeness { get; }
}
