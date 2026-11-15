using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sprechen.Forms.Pages.Models;
using Sprechen.Forms.Routing;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Web.FormSurfaces;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Verarbeitet die Formular-Startseite und übernimmt Katalogauswahl,
/// Studienidentität und Formularanzeige aus dem Seitenablauf.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly IndexPageRuntime _runtime;

    public IndexModel(IndexPageRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    [BindProperty(SupportsGet = true, Name = "lang")]
    public string? Language { get; init; }

    [BindProperty(SupportsGet = true, Name = "app")]
    public string? ApplicationId { get; init; }

    [BindProperty(SupportsGet = true, Name = "form")]
    public string? FormId { get; init; }

    [BindProperty(SupportsGet = true, Name = "prevApp")]
    public string? PreviousApplicationId { get; init; }

    /// <summary>
    /// Grund für einen abgelehnten Studienlink: token für eine ungültige Kennung
    /// oder segments für zusätzliche Pfadteile.
    /// </summary>
    public string? StudyLinkRejectionReason { get; private set; }

    public bool IsStudyLinkRejected => StudyLinkRejectionReason is not null;

    public string StudyLinkRejectionHeading => FormsStudyLinkRejectionCopy.Heading(SelectedLanguage);

    public string StudyLinkRejectionMessage =>
        FormsStudyLinkRejectionCopy.Message(SelectedLanguage, StudyLinkRejectionReason);

    public IReadOnlyList<string> Languages { get; private set; } = Array.Empty<string>();

    public string SelectedLanguage { get; private set; } = "de";

    public IReadOnlyList<ApplicationOption> Applications { get; private set; } = Array.Empty<ApplicationOption>();

    public string SelectedApplicationId { get; private set; } = string.Empty;

    public IReadOnlyList<FormOption> Forms { get; private set; } = Array.Empty<FormOption>();

    public string SelectedFormId { get; private set; } = string.Empty;

    public IReadOnlyList<FormSurfaceField> Fields { get; private set; } = Array.Empty<FormSurfaceField>();

    public IReadOnlyDictionary<string, FormSurfaceFieldGroupInfo> FieldGroupsById { get; private set; } =
        new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase);

    public string EntryFieldGroupId { get; private set; } = string.Empty;

    public string FormContractJson { get; private set; } = string.Empty;

    public string NavigationManifestJson { get; private set; } = "{}";

    public string FormDisplayName { get; private set; } = string.Empty;

    public string FormDescription { get; private set; } = string.Empty;

    public FormSurfacePresentation? Surface { get; private set; }

    public string? ErrorCode { get; private set; }

    /// <summary>
    /// Optionaler Fehlertext für einen im Textkatalog unbekannten Fehlercode.
    /// Die aktuellen Ladepfade setzen nur ErrorCode. Diese Eigenschaft bleibt dabei leer.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Aufgelöstes HTML-Projektionsprofil des geladenen Vertrags. Vorgabe ist
    /// <see cref="HtmlProjectionProfiles.A11yFirst"/>. Ein unbekanntes Profil
    /// lehnt <see cref="HtmlProjectionProfileResolver"/> mit dem festen Fehlercode
    /// <c>E_HTML_PROJECTION_PROFILE_UNSUPPORTED</c> ab, bevor Felder gebaut werden.
    /// </summary>
    public string HtmlProjectionProfile { get; private set; } = HtmlProjectionProfiles.A11yFirst;

    /// <summary>
    /// Teilnehmerkennung aus dem geprüften Studienlink oder den Studienparametern.
    /// </summary>
    public string StudyParticipantId { get; private set; } = string.Empty;


    public string StudyTaskId { get; private set; } = string.Empty;


    public string StudyCondition { get; private set; } = "A";

    public bool IsOperationalProbe { get; private set; }

    /// <summary>
    /// Einsbasierter Formularindex, damit die Ansage den Fortschritt als Schritt X von Y nennen kann.
    /// </summary>
    public int CurrentFormIndex { get; private set; }


    public int TotalFormsCount { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var identity = HttpContext?.Features.Get<StudyLinkIdentity>();
        IsOperationalProbe = identity?.IsOperationalProbe == true;
        if (identity is { IsRejected: true })
        {
            SelectedLanguage = string.IsNullOrWhiteSpace(Language)
                ? "de"
                : Language.Trim().ToLowerInvariant();
            StudyLinkRejectionReason = identity.RejectionReason;
            return;
        }

        var result = await _runtime.LoadAsync(
                new IndexPageWorkflowRequest(
                    Request,
                    Language,
                    ApplicationId,
                    FormId,
                    PreviousApplicationId,
                    new IndexPageEntryAdmission(identity?.Token, IsOperationalProbe)),
                cancellationToken)
            .ConfigureAwait(false);

        ApplyWorkflowResult(result);
        if (identity?.Token is { Length: > 0 } token)
        {
            StudyParticipantId = token;
        }
    }

    private void ApplyWorkflowResult(IndexPageWorkflowResult result)
    {
        ApplyStudyParameters(result.StudyParameters);
        ApplySelectionIfPresent(result.Selection);
        ApplyLoadedFormIfPresent(result.LoadedForm);
        ApplyWorkflowError(result);
    }

    private void ApplySelectionIfPresent(IndexPageSelectionResult? selection)
    {
        if (selection is not null)
        {
            ApplySelection(selection);
        }
    }

    private void ApplyLoadedFormIfPresent(IndexPageSelectedFormResult? loadedForm)
    {
        if (loadedForm is not null)
        {
            ApplyLoadedForm(loadedForm);
        }
    }

    private void ApplyWorkflowError(IndexPageWorkflowResult result)
    {
        ErrorCode = result.ErrorCode ?? ErrorCode;
    }

    private void ApplyStudyParameters(IndexPageStudyParameters studyParameters)
    {
        StudyParticipantId = studyParameters.ParticipantId;
        StudyTaskId = studyParameters.TaskId;
        StudyCondition = studyParameters.Condition;
    }

    private void ApplySelection(IndexPageSelectionResult selection)
    {
        Languages = selection.Languages;
        SelectedLanguage = selection.SelectedLanguage;
        Applications = selection.Applications;
        SelectedApplicationId = selection.SelectedApplicationId;
        Forms = selection.Forms;
        SelectedFormId = selection.SelectedFormId;
        NavigationManifestJson = selection.NavigationManifestJson;
        TotalFormsCount = selection.TotalFormsCount;
        CurrentFormIndex = selection.CurrentFormIndex;
    }

    private void ApplyLoadedForm(IndexPageSelectedFormResult loadedForm)
    {
        EntryFieldGroupId = loadedForm.EntryFieldGroupId;
        FieldGroupsById = loadedForm.FieldGroupsById;
        Fields = loadedForm.Fields;
        Surface = loadedForm.Surface;
        FormContractJson = loadedForm.FormContractJson;
        FormDisplayName = loadedForm.FormDisplayName;
        FormDescription = loadedForm.FormDescription;
        HtmlProjectionProfile = loadedForm.HtmlProjectionProfile;
        ErrorCode = loadedForm.ErrorCode;
    }

    public async Task<IActionResult> OnGetFormsAsync(string? app, string? lang, CancellationToken cancellationToken)
    {
        return await _runtime.GetFormsAsync(Request, app, lang, cancellationToken).ConfigureAwait(false);
    }
}
