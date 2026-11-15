using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Sprach-, Ansage- und Erfassungsvorgaben des Katalogdialogs gemeinsam bereit. Die
/// einzelnen Katalogschritte verwenden dadurch denselben Prompt-Kontext und dieselben
/// Interaktionseinstellungen.
/// </summary>
internal sealed record FormsCatalogDialogPromptRuntime
{
    public FormsCatalogDialogPromptRuntime(
        CatalogPromptContext promptContext,
        SelectionDialogInteractionSettings interactionSettings,
        PolitenessLevel politenessLevel,
        string isoCode,
        int noInputTimeoutMs,
        int maxRetries)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(interactionSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentOutOfRangeException.ThrowIfNegative(noInputTimeoutMs);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        PromptContext = promptContext;
        InteractionSettings = interactionSettings;
        PolitenessLevel = politenessLevel;
        IsoCode = isoCode;
        NoInputTimeoutMs = noInputTimeoutMs;
        MaxRetries = maxRetries;
    }

    public CatalogPromptContext PromptContext { get; }

    public SelectionDialogInteractionSettings InteractionSettings { get; }

    public PolitenessLevel PolitenessLevel { get; }

    public string IsoCode { get; }

    public int NoInputTimeoutMs { get; }

    public int MaxRetries { get; }
}
