using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Bestätigungsvorgaben, Kennungen abgeschlossener Formulare und die Namen der verfügbaren
/// Formulare für den Auswahlübergang. Damit lassen sich noch fehlende vorausgesetzte Formulare benennen.
/// </summary>
internal sealed record FormSelectionTransitionContext
{
    public FormSelectionTransitionContext(
        ConfirmationPolicy confirmationPolicy,
        IReadOnlySet<string> completedFormIds,
        IReadOnlyDictionary<string, string> formNamesById,
        CatalogPromptContext promptContext,
        PolitenessLevel politenessLevel,
        string isoCode)
    {
        ArgumentNullException.ThrowIfNull(completedFormIds);
        ArgumentNullException.ThrowIfNull(formNamesById);
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        ConfirmationPolicy = confirmationPolicy;
        CompletedFormIds = completedFormIds;
        FormNamesById = formNamesById;
        PromptContext = promptContext;
        PolitenessLevel = politenessLevel;
        IsoCode = isoCode;
    }

    public ConfirmationPolicy ConfirmationPolicy { get; }

    public IReadOnlySet<string> CompletedFormIds { get; }

    public IReadOnlyDictionary<string, string> FormNamesById { get; }

    public CatalogPromptContext PromptContext { get; }

    public PolitenessLevel PolitenessLevel { get; }

    public string IsoCode { get; }
}
