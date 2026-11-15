using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet den aktuellen Kandidatenstand mit Katalog, Ansagevorgaben und Intent-Behandlung.
/// Katalogbefehle erhalten damit den Kontext der laufenden Rückfrage statt eines neu aufgebauten
/// Bestands.
/// </summary>
internal sealed record FormsCatalogDialogTurnContext
{
    public FormsCatalogDialogTurnContext(
        SelectionDialogCommonIntentHandler<ApplicationInfo> intentHandler,
        FormsCatalog catalog,
        string correlationId,
        FormsCatalogDialogTurnSelection selection,
        FormsCatalogDialogPromptRuntime promptRuntime)
    {
        ArgumentNullException.ThrowIfNull(intentHandler);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(promptRuntime);

        IntentHandler = intentHandler;
        Catalog = catalog;
        CorrelationId = correlationId;
        Selection = selection;
        PromptRuntime = promptRuntime;
    }

    public SelectionDialogCommonIntentHandler<ApplicationInfo> IntentHandler { get; }

    public FormsCatalog Catalog { get; }

    public string CorrelationId { get; }

    public FormsCatalogDialogTurnSelection Selection { get; }

    public FormsCatalogDialogPromptRuntime PromptRuntime { get; }

    public IReadOnlyList<ApplicationInfo> Applications => Selection.Applications;

    public IReadOnlyList<ApplicationInfo> VisibleApplications => Selection.VisibleApplications;

    public SelectionDialogTurnSnapshot<ApplicationInfo> Candidates => Selection.Candidates;

    public SelectionCandidateSnapshot<ApplicationInfo> InitialCandidates => Candidates.InitialCandidates;

    public SelectionCandidateSnapshot<ApplicationInfo> VisibleCandidates => Candidates.VisibleCandidates;

    public SelectionCandidateSnapshot<ApplicationInfo> SelectableCandidates => Candidates.SelectableCandidates;

    public ApplicationInfo? CurrentSelection => Selection.CurrentSelection;

    public SelectionDialogPrompt? LastPrompt => Selection.LastPrompt;

    public CatalogPromptContext PromptContext => PromptRuntime.PromptContext;

    public SelectionDialogInteractionSettings InteractionSettings => PromptRuntime.InteractionSettings;

    public PolitenessLevel PolitenessLevel => PromptRuntime.PolitenessLevel;

    public string IsoCode => PromptRuntime.IsoCode;

    public int NoInputTimeoutMs => PromptRuntime.NoInputTimeoutMs;

    public int MaxRetries => PromptRuntime.MaxRetries;
}
