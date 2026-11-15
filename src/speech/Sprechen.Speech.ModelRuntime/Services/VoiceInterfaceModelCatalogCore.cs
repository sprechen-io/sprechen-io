using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Bündelt den aufgelösten Dialogmodellstand mit Lexikon-, Aktivierungswort-
/// und Textkatalogen, damit die Teilkataloge aus derselben Modellinstanz entstehen.
/// Stellt auch die zugehörigen Sprachvorgaben bereit.
/// </summary>
internal sealed class VoiceInterfaceModelCatalogCore
{
    private readonly VoiceInterface _model;

    public VoiceInterfaceModelCatalogCore(VoiceInterface model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        Lexicon = new VoiceLexiconModelCatalog(_model);
        WakeWord = new VoiceWakeWordModelCatalog(_model);
        Resources = new VoiceResourceTextModelCatalog(_model);
    }

    public string DefaultLanguage => _model.DefaultLanguage;

    public IReadOnlyList<string> SupportedLanguages => _model.SupportedLanguages;

    /// <summary>
    /// Gibt die Modellvorgabe zurück, ob der Sitzungseinstieg eine geführte
    /// Sprachentscheidung anbietet. Bei false entfällt dieses Sprachangebot.
    /// </summary>
    public bool SessionLanguageOfferEnabled => _model.SessionLanguageOfferEnabled;

    public TimeSpan FirstPcmEnqueueTimeout => TimeSpan.FromMilliseconds(_model.FirstPcmEnqueueTimeoutMs);

    public EarconId FirstPcmTimeoutEarconId => Enum.Parse<EarconId>(_model.FirstPcmTimeoutEarconId, ignoreCase: false);

    public VoiceLexiconModelCatalog Lexicon { get; }

    public VoiceWakeWordModelCatalog WakeWord { get; }

    public VoiceResourceTextModelCatalog Resources { get; }
}
