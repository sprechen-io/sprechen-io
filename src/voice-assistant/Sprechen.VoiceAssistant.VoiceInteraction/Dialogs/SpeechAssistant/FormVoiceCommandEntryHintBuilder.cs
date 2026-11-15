namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ergänzt die Feldhilfe um passende Eingabehinweise für Datum, Uhrzeit,
/// Mehrfachauswahl sowie Text- und Codefelder. Ausführliche Erklärungen
/// können bei Bedarf erneut angefordert werden.
/// </summary>
internal static class FormVoiceCommandEntryHintBuilder
{
    public static async Task<string?> BuildAsync(FormVoiceCommandFieldTurnContext turnContext)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        var field = turnContext.Field;
        var shouldReteachStructuralHint = turnContext.StateTracker.ConsumeStructuralTeachingReteach(field.FieldId);
        var effectiveInteractionType =
            FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, field.ValueTypeKind);

        if (string.Equals(effectiveInteractionType, EffectiveFieldInteractionTypes.Date, StringComparison.Ordinal))
        {
            return BuildDateEntryHint(turnContext, shouldReteachStructuralHint);
        }

        if (string.Equals(effectiveInteractionType, EffectiveFieldInteractionTypes.Time, StringComparison.Ordinal))
        {
            return BuildTimeEntryHint(turnContext, shouldReteachStructuralHint);
        }

        if (string.Equals(effectiveInteractionType, EffectiveFieldInteractionTypes.MultiSelect, StringComparison.Ordinal))
        {
            return BuildMultiSelectEntryHint(turnContext, shouldReteachStructuralHint);
        }

        if (effectiveInteractionType is EffectiveFieldInteractionTypes.Text or EffectiveFieldInteractionTypes.Code)
        {
            return await BuildSpellingEntryHintAsync(turnContext).ConfigureAwait(false);
        }

        return field.HelpText;
    }

    private static string? BuildDateEntryHint(
        FormVoiceCommandFieldTurnContext turnContext,
        bool shouldReteachStructuralHint)
    {
        var context = turnContext.Context;
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        var teachDateStructure =
            shouldReteachStructuralHint
            || !turnContext.StateTracker.HasAlreadyTaught(DialogTeachOnceTopics.DateStructure);

        var dateHint = teachDateStructure
            ? FormDateEntryExamplePromptBuilder.GetDateEntryDecompositionHintPrompt(
                  context.Contract,
                  context.IsoCode,
                  politenessLevel)
              ?? FormDateEntryExamplePromptBuilder.GetDateEntryExamplePrompt(
                  context.Contract,
                  context.IsoCode,
                  politenessLevel)
            : FormDateEntryExamplePromptBuilder.GetDateEntryExamplePrompt(
                  context.Contract,
                  context.IsoCode,
                  politenessLevel);

        if (teachDateStructure && !string.IsNullOrWhiteSpace(dateHint))
        {
            var componentEntryMention = FormDateComponentPromptBuilder.GetDateComponentEntryAvailablePrompt(
                context.Contract,
                context.IsoCode,
                politenessLevel);
            dateHint = ComposeEntryHint(dateHint, componentEntryMention);
        }

        return ComposeEntryHint(turnContext.Field.HelpText, dateHint);
    }

    private static string? BuildTimeEntryHint(
        FormVoiceCommandFieldTurnContext turnContext,
        bool shouldReteachStructuralHint)
    {
        var context = turnContext.Context;
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        var teachTimeStructure =
            shouldReteachStructuralHint
            || !turnContext.StateTracker.HasAlreadyTaught(DialogTeachOnceTopics.TimeStructure);

        var timeHint = teachTimeStructure
            ? FormTimeEntryExamplePromptBuilder.GetTimeEntryStructureHintPrompt(
                  context.Contract,
                  context.IsoCode,
                  politenessLevel)
              ?? FormTimeEntryExamplePromptBuilder.GetTimeEntryExamplePrompt(
                  context.Contract,
                  context.IsoCode,
                  politenessLevel)
            : FormTimeEntryExamplePromptBuilder.GetTimeEntryExamplePrompt(
                  context.Contract,
                  context.IsoCode,
                  politenessLevel);

        return ComposeEntryHint(turnContext.Field.HelpText, timeHint);
    }

    private static string? BuildMultiSelectEntryHint(
        FormVoiceCommandFieldTurnContext turnContext,
        bool shouldReteachStructuralHint)
    {
        var context = turnContext.Context;
        var field = turnContext.Field;
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        var setStructureHint =
            !shouldReteachStructuralHint
            && turnContext.StateTracker.HasAlreadyTaught(DialogTeachOnceTopics.SetAccumulation)
                ? null
                : FormSetWorkflowPromptBuilder.GetSetAccumulationStructurePrompt(
                      context.Contract,
                      context.IsoCode,
                      politenessLevel);

        var selectionHint = field.MaxSelected is (int maxSelected and > 1)
            ? FormSetWorkflowPromptBuilder.GetSetSelectionHintPrompt(
                  context.Contract,
                  context.IsoCode,
                  field.MinSelected ?? 1,
                  maxSelected,
                  politenessLevel)
            : null;

        return ComposeEntryHint(field.HelpText, setStructureHint, selectionHint);
    }

    private static async Task<string?> BuildSpellingEntryHintAsync(FormVoiceCommandFieldTurnContext turnContext)
    {
        var spellOutAllowed = await turnContext.PrivacyReadbackPolicy
            .IsSpellOutAllowedInConfirmationAsync(turnContext.Field.VariableId)
            .ConfigureAwait(false);

        // Bei bereits aktivem Buchstabiermodus keinen erneuten Wechsel dorthin
        // anbieten. Der Hinweis ist nur vor dem Einstieg sinnvoll.
        var spellingOverrideHint =
            spellOutAllowed
            && turnContext.Field.InputMode != InputMode.Spelling
            && !turnContext.StateTracker.HasAlreadyTaught(DialogTeachOnceTopics.SpellingOverrideAvailable)
                ? FormSpellingOverrideDiscoveryPromptBuilder.GetSpellingOverrideAvailablePrompt(
                      turnContext.Context.Contract,
                      turnContext.Context.IsoCode,
                      turnContext.Context.VoiceSettings.GetPolitenessLevel())
                : null;

        return ComposeEntryHint(turnContext.Field.HelpText, spellingOverrideHint);
    }

    private static string? ComposeEntryHint(string? helpText, params string?[] additions)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(helpText))
        {
            parts.Add(helpText.Trim());
        }

        foreach (var addition in additions)
        {
            if (!string.IsNullOrWhiteSpace(addition))
            {
                parts.Add(addition.Trim());
            }
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }
}
