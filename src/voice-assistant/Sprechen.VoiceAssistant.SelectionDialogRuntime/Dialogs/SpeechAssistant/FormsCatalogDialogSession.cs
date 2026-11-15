using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Anwendungsauswahl, Katalog und Ansagevorgaben für eine Dialogsitzung zusammen. Die
/// Ausführungsbausteine beziehen Sprache und Interaktionseinstellungen dadurch aus demselben
/// Kontext.
/// </summary>
internal sealed record FormsCatalogDialogSession
{
    public FormsCatalogDialogSession(
        FormsCatalog catalog,
        List<ApplicationInfo> applications,
        CatalogPromptContext promptContext,
        FormsCatalogDialogPromptRuntime promptRuntime,
        ConfirmationPolicy confirmationPolicy)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(applications);
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(promptRuntime);

        Catalog = catalog;
        Applications = applications;
        PromptContext = promptContext;
        PromptRuntime = promptRuntime;
        ConfirmationPolicy = confirmationPolicy;
    }

    public FormsCatalog Catalog { get; }

    public List<ApplicationInfo> Applications { get; }

    public CatalogPromptContext PromptContext { get; }

    public FormsCatalogDialogPromptRuntime PromptRuntime { get; }

    public ConfirmationPolicy ConfirmationPolicy { get; }

    public SelectionDialogInteractionSettings InteractionSettings => PromptRuntime.InteractionSettings;

    public PolitenessLevel PolitenessLevel => PromptRuntime.PolitenessLevel;

    public string IsoCode => PromptRuntime.IsoCode;

    public int NoInputTimeoutMs => PromptRuntime.NoInputTimeoutMs;

    public int MaxRetries => PromptRuntime.MaxRetries;
}
