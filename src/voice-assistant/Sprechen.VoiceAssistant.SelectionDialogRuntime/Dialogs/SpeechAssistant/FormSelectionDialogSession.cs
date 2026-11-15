using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Formularbestand, erfüllte Voraussetzungen und Ansagevorgaben einer Auswahl zusammen. Die
/// beteiligten Fabriken greifen dadurch auf denselben Sitzungskontext zu.
/// </summary>
internal sealed record FormSelectionDialogSession
{
    public FormSelectionDialogSession(
        FormsCatalog catalog,
        List<FormInfo> forms,
        CatalogPromptContext promptContext,
        SelectionDialogInteractionSettings interactionSettings,
        PolitenessLevel politenessLevel,
        string isoCode,
        int noInputTimeoutMs,
        int maxRetries,
        FormSelectionTransitionContext transitionContext)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(forms);
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(interactionSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentOutOfRangeException.ThrowIfNegative(noInputTimeoutMs);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentNullException.ThrowIfNull(transitionContext);

        Catalog = catalog;
        Forms = forms;
        PromptContext = promptContext;
        InteractionSettings = interactionSettings;
        PolitenessLevel = politenessLevel;
        IsoCode = isoCode;
        NoInputTimeoutMs = noInputTimeoutMs;
        MaxRetries = maxRetries;
        TransitionContext = transitionContext;
    }

    public FormsCatalog Catalog { get; }

    public List<FormInfo> Forms { get; }

    public CatalogPromptContext PromptContext { get; }

    public SelectionDialogInteractionSettings InteractionSettings { get; }

    public PolitenessLevel PolitenessLevel { get; }

    public string IsoCode { get; }

    public int NoInputTimeoutMs { get; }

    public int MaxRetries { get; }

    public FormSelectionTransitionContext TransitionContext { get; }
}
