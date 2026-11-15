using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft vorhandene Ansagen, Kommandoformulierungen und Feldsynonyme
/// auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class FormVoiceHintsLocalizationCoverageValidator
{
    internal static void Validate(
        VoiceHintsDefinition? hints,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (hints is null)
        {
            return;
        }

        ValidatePrompts(hints.Prompts, language, code, issues);
        ValidateIntents(hints.Intents, language, code, issues);
        ValidateFieldNameSynonyms(hints.FieldNameSynonyms, language, code, issues);
        ValidateGlobalCommands(hints.GlobalCommands, language, code, issues);
    }

    private static void ValidatePrompts(
        IReadOnlyList<VoicePrompt>? prompts,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (prompts is null)
        {
            return;
        }

        for (var i = 0; i < prompts.Count; i++)
        {
            FormLocalizedTextCoverageRule.Validate(
                prompts[i].Text,
                language,
                $"$.projections.voice.hints.prompts[{i}].text",
                $"voice prompt '{prompts[i].PromptId}' text",
                issues,
                prompts[i].PromptId,
                code);
        }
    }

    private static void ValidateIntents(
        IReadOnlyList<VoiceIntent>? intents,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (intents is null)
        {
            return;
        }

        for (var i = 0; i < intents.Count; i++)
        {
            var intent = intents[i];
            for (var j = 0; j < intent.Utterances.Count; j++)
            {
                FormLocalizedTextCoverageRule.Validate(
                    intent.Utterances[j],
                    language,
                    $"$.projections.voice.hints.intents[{i}].utterances[{j}]",
                    $"voice intent '{intent.IntentId}' utterance",
                    issues,
                    intent.IntentId,
                    code);
            }
        }
    }

    private static void ValidateFieldNameSynonyms(
        IReadOnlyDictionary<string, SynonymSet>? fieldNameSynonyms,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (fieldNameSynonyms is null)
        {
            return;
        }

        foreach (var pair in fieldNameSynonyms)
        {
            if (pair.Value is null)
            {
                continue;
            }

            ValidateFieldNameSynonym(pair.Key, pair.Value, language, code, issues);
        }
    }

    private static void ValidateFieldNameSynonym(
        string fieldId,
        SynonymSet synonyms,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        FormLocalizedTextCoverageRule.Validate(
            synonyms.PrimaryName,
            language,
            $"$.projections.voice.hints.fieldNameSynonyms['{fieldId}'].primaryName",
            $"fieldNameSynonyms primaryName for '{fieldId}'",
            issues,
            fieldId,
            code);

        if (synonyms.Synonyms is null)
        {
            return;
        }

        for (var i = 0; i < synonyms.Synonyms.Count; i++)
        {
            FormLocalizedTextCoverageRule.Validate(
                synonyms.Synonyms[i],
                language,
                $"$.projections.voice.hints.fieldNameSynonyms['{fieldId}'].synonyms[{i}]",
                $"fieldNameSynonyms synonym {i} for '{fieldId}'",
                issues,
                fieldId,
                code);
        }
    }

    private static void ValidateGlobalCommands(
        IReadOnlyList<GlobalCommandDefinition>? globalCommands,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (globalCommands is null)
        {
            return;
        }

        for (var i = 0; i < globalCommands.Count; i++)
        {
            var command = globalCommands[i];
            for (var j = 0; j < command.Utterances.Count; j++)
            {
                FormLocalizedTextCoverageRule.Validate(
                    command.Utterances[j],
                    language,
                    $"$.projections.voice.hints.globalCommands[{i}].utterances[{j}]",
                    $"global command '{command.CommandId}' utterance",
                    issues,
                    command.CommandId,
                    code);
            }
        }
    }
}
