using System.Globalization;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt gemeinsame Bestätigungs- und Auswahlansagen aus den dafür modellierten
/// Texten. Anwendungs- und Formularauswahl teilen sich Parameteraufbau und
/// Zusammenstellung der Ansagen, liefern aber ihre eigenen Modellschlüssel.
/// </summary>
internal sealed class SelectionCommonIntentPromptBuilder<TSelection>
    where TSelection : class
{
    private readonly SelectionCommonIntentPromptIds ids;
    private readonly Func<TSelection, string> displayNameOf;

    internal SelectionCommonIntentPromptBuilder(
        SelectionCommonIntentPromptIds ids,
        Func<TSelection, string> displayNameOf)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(displayNameOf);
        ids.RequireEveryId();
        this.ids = ids;
        this.displayNameOf = displayNameOf;
    }

    internal SelectionDialogPrompt ResolveConfirmationPrompt(
        CatalogPromptContext promptContext,
        SelectionConfirmationPromptRequest<TSelection> request,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(request);
        var candidate = request.Candidate;

        return request.Kind switch
        {
            SelectionConfirmationPromptKind.InitialConfirmation =>
                GetConfirmationPrompt(promptContext, candidate.Item, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.LowConfidenceConfirmation =>
                GetLowConfidenceConfirmationPrompt(promptContext, candidate.Item, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.ExplicitAuthorization =>
                GetExplicitAuthorizationPrompt(promptContext, candidate, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.VerificationUnavailable =>
                GetVerificationUnavailablePrompt(promptContext, candidate, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.NotOpenedReselect =>
                GetNotOpenedReselectPrompt(promptContext, candidate, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.Cancellation =>
                GetCancelPrompt(promptContext, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.ContextualHelp =>
                GetContextualConfirmationHelpPrompt(promptContext, candidate, request.Stage, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.NoInputReminder =>
                GetConfirmationRepeatPrompt(promptContext, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.ActiveStage => request.ActiveStagePrompt!,
            SelectionConfirmationPromptKind.ActiveStageList =>
                GetConfirmationListPrompt(
                    promptContext, request.RetainedCandidates, request.ActiveStagePrompt!, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.VerificationFailedTerminal =>
                GetVerificationFailedTerminalPrompt(promptContext, candidate, isoCode, politenessLevel),
            SelectionConfirmationPromptKind.RepairExhaustedTerminal =>
                GetRepairExhaustedTerminalPrompt(promptContext, candidate, isoCode, politenessLevel),
            _ => throw new ArgumentOutOfRangeException(
                nameof(request), request.Kind, "Unknown selection confirmation prompt kind.")
        };
    }

    internal SelectionDialogPrompt GetConfirmationPrompt(
        CatalogPromptContext promptContext, TSelection item, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.Confirmation, isoCode, politenessLevel, Name(item));

    internal SelectionDialogPrompt GetLowConfidenceConfirmationPrompt(
        CatalogPromptContext promptContext, TSelection item, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.LowConfidenceConfirm, isoCode, politenessLevel, Name(item));

    internal SelectionDialogPrompt GetExplicitAuthorizationPrompt(
        CatalogPromptContext promptContext, SelectionCandidate<TSelection> candidate, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.ExplicitAuthorization, isoCode, politenessLevel, Name(candidate), Number(candidate));

    internal SelectionDialogPrompt GetVerificationUnavailablePrompt(
        CatalogPromptContext promptContext, SelectionCandidate<TSelection> candidate, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.VerificationUnavailable, isoCode, politenessLevel, Name(candidate), Number(candidate));

    internal SelectionDialogPrompt GetContextualConfirmationHelpPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<TSelection> candidate,
        SelectionConfirmationCaptureStage stage,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var promptId = stage switch
        {
            SelectionConfirmationCaptureStage.Initial => ids.InitialConfirmationHelp,
            SelectionConfirmationCaptureStage.ExplicitAuthorization => ids.ExplicitAuthorizationHelp,
            SelectionConfirmationCaptureStage.ConfidenceFaultRetry => ids.ConfidenceFaultRetryHelp,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown selection confirmation-help stage.")
        };

        // Die erste Bestätigungshilfe nennt den Kandidaten ohne Nummer, weil eine
        // nummerngebundene Freigabe erst in den späteren Bestätigungsstufen vorgesehen ist.
        return stage == SelectionConfirmationCaptureStage.Initial
            ? Resolve(promptContext, promptId, isoCode, politenessLevel, Name(candidate))
            : Resolve(promptContext, promptId, isoCode, politenessLevel, Name(candidate), Number(candidate));
    }

    internal SelectionDialogPrompt GetNotOpenedReselectPrompt(
        CatalogPromptContext promptContext, SelectionCandidate<TSelection> candidate, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.NotOpenedReselect, isoCode, politenessLevel, Name(candidate));

    internal SelectionDialogPrompt GetVerificationFailedTerminalPrompt(
        CatalogPromptContext promptContext, SelectionCandidate<TSelection> candidate, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.VerificationFailedTerminal, isoCode, politenessLevel, Name(candidate));

    internal SelectionDialogPrompt GetRepairExhaustedTerminalPrompt(
        CatalogPromptContext promptContext, SelectionCandidate<TSelection> candidate, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.RepairExhaustedTerminal, isoCode, politenessLevel, Name(candidate));

    internal SelectionDialogPrompt GetCaptureFailedTerminalPrompt(
        CatalogPromptContext promptContext, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.CaptureFailedTerminal, isoCode, politenessLevel);

    internal SelectionDialogPrompt GetConfirmationListPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidateSnapshot<TSelection> retainedCandidates,
        SelectionDialogPrompt activeStagePrompt,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(retainedCandidates);
        ArgumentNullException.ThrowIfNull(activeStagePrompt);
        if (retainedCandidates.Count == 0)
        {
            throw new ArgumentException(
                "A confirmation list requires the retained candidate inventory.",
                nameof(retainedCandidates));
        }

        var introText = Resolve(promptContext, ids.ListIntro, isoCode, politenessLevel, Count(retainedCandidates.Count)).Text;
        var itemTexts = retainedCandidates.Candidates
            .Select(candidate =>
                Resolve(promptContext, ids.ListItem, isoCode, politenessLevel, Name(candidate), Number(candidate)).Text);

        return SelectionDialogPrompt.FromSequence(introText, itemTexts, activeStagePrompt.Text);
    }

    internal SelectionDialogPrompt GetConfirmationRepeatPrompt(
        CatalogPromptContext promptContext, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.ConfirmationRepeat, isoCode, politenessLevel);

    internal SelectionDialogPrompt GetLaunchPrompt(
        CatalogPromptContext promptContext, TSelection item, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.Launch, isoCode, politenessLevel, Name(item));

    internal SelectionDialogPrompt GetHelpPrompt(CatalogPromptContext promptContext, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.Help, isoCode, politenessLevel);

    internal SelectionDialogPrompt GetCancelPrompt(CatalogPromptContext promptContext, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.Cancelled, isoCode, politenessLevel);

    internal SelectionDialogPrompt GetUnrecognizedPrompt(CatalogPromptContext promptContext, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.Unrecognized, isoCode, politenessLevel);

    internal SelectionDialogPrompt GetNoInputPrompt(CatalogPromptContext promptContext, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.NoInput, isoCode, politenessLevel);

    internal SelectionDialogPrompt GetNoItemsPrompt(CatalogPromptContext promptContext, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.NoItems, isoCode, politenessLevel);

    internal SelectionDialogPrompt GetNoMatchPrompt(
        CatalogPromptContext promptContext, string? name, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(
            promptContext,
            ids.NoMatch,
            isoCode,
            politenessLevel,
            (CatalogPromptTemplateParameterNames.Name, string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim()));

    internal SelectionDialogPrompt GetInvalidNumberPrompt(
        CatalogPromptContext promptContext, int max, string isoCode, PolitenessLevel politenessLevel)
        => Resolve(promptContext, ids.InvalidNumber, isoCode, politenessLevel, (CatalogPromptTemplateParameterNames.Max, Invariant(max)));

    private (string Key, string? Value) Name(TSelection item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return (CatalogPromptTemplateParameterNames.Name, displayNameOf(item));
    }

    private static (string Key, string? Value) Name(SelectionCandidate<TSelection> candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return (CatalogPromptTemplateParameterNames.Name, candidate.DisplayName);
    }

    private static (string Key, string? Value) Number(SelectionCandidate<TSelection> candidate)
        => (CatalogPromptTemplateParameterNames.Number, Invariant(candidate.StableNumber));

    private static (string Key, string? Value) Count(int count)
        => (CatalogPromptTemplateParameterNames.Count, Invariant(count));

    private static string Invariant(int value)
        => value.ToString(CultureInfo.InvariantCulture);

    // Dieser Auflösungsschritt setzt eine modellierte Prompt-Rolle und ihre Parameter gemeinsam
    // um. Das Parameterwörterbuch wird dadurch an einer Stelle statt in jeder Ansagefunktion
    // aufgebaut.
    private static SelectionDialogPrompt Resolve(
        CatalogPromptContext promptContext,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        params (string Key, string? Value)[] parameters)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        var templateParameters = parameters.Length == 0
            ? null
            : parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(promptContext, promptId, isoCode, politenessLevel, templateParameters));
    }
}
