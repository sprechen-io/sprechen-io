using Sprechen.Mda.Contracts.V1.FormVoice;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Übernimmt die HTML-Projektion und kopiert Sprachhinweise sowie das übergebene
/// Sprachdialogprofil in die Laufzeitdarstellung des Formularvertrags.
/// </summary>
internal static class FormContractProjectionResolver
{
    internal static ContractProjections? Resolve(
        ContractProjectionsDocument? projections,
        FormVoiceProfile? formVoiceProfile)
    {
        if (projections is null)
        {
            return null;
        }

        return new ContractProjections
        {
            Html = projections.Html,
            Voice = CloneVoiceProjection(projections.Voice, formVoiceProfile)
        };
    }

    private static VoiceProjection? CloneVoiceProjection(
        VoiceProjectionDocument? projection,
        FormVoiceProfile? formVoiceProfile)
    {
        if (projection is null)
        {
            return null;
        }

        return new VoiceProjection
        {
            Hints = CloneVoiceHintsDefinition(projection.Hints),
            FormVoiceProfile = CloneFormVoiceProfile(formVoiceProfile)
        };
    }

    private static VoiceHintsDefinition? CloneVoiceHintsDefinition(VoiceHintsDefinition? hints)
    {
        if (hints is null)
        {
            return null;
        }

        return new VoiceHintsDefinition
        {
            Prompts = CloneVoicePrompts(hints.Prompts),
            Intents = CloneVoiceIntents(hints.Intents),
            FieldNameSynonyms = CloneSynonymSets(hints.FieldNameSynonyms),
            CommandModelId = hints.CommandModelId,
            GlobalCommands = CloneGlobalCommands(hints.GlobalCommands)
        };
    }

    private static List<VoicePrompt>? CloneVoicePrompts(List<VoicePrompt>? prompts)
    {
        if (prompts is null)
        {
            return null;
        }

        var clones = new List<VoicePrompt>(prompts.Count);
        foreach (var prompt in prompts)
        {
            clones.Add(new VoicePrompt
            {
                PromptId = prompt.PromptId,
                Text = FormContractResolutionJsonCloner.CloneRequiredLocalizedText(prompt.Text)
            });
        }

        return clones;
    }

    private static List<VoiceIntent>? CloneVoiceIntents(List<VoiceIntent>? intents)
    {
        if (intents is null)
        {
            return null;
        }

        var clones = new List<VoiceIntent>(intents.Count);
        foreach (var intent in intents)
        {
            clones.Add(new VoiceIntent
            {
                IntentId = intent.IntentId,
                Utterances = FormContractResolutionJsonCloner.CloneLocalizedTextList(intent.Utterances) ?? []
            });
        }

        return clones;
    }

    private static Dictionary<string, SynonymSet>? CloneSynonymSets(Dictionary<string, SynonymSet>? synonyms)
    {
        if (synonyms is null)
        {
            return null;
        }

        var clone = new Dictionary<string, SynonymSet>(synonyms.Count, StringComparer.Ordinal);
        foreach (var pair in synonyms)
        {
            clone[pair.Key] = new SynonymSet
            {
                PrimaryName = FormContractResolutionJsonCloner.CloneRequiredLocalizedText(pair.Value.PrimaryName),
                Synonyms = FormContractResolutionJsonCloner.CloneLocalizedTextList(pair.Value.Synonyms)
            };
        }

        return clone;
    }

    private static List<GlobalCommandDefinition>? CloneGlobalCommands(List<GlobalCommandDefinition>? commands)
    {
        if (commands is null)
        {
            return null;
        }

        var clones = new List<GlobalCommandDefinition>(commands.Count);
        foreach (var command in commands)
        {
            clones.Add(new GlobalCommandDefinition
            {
                CommandId = command.CommandId,
                Utterances = FormContractResolutionJsonCloner.CloneLocalizedTextList(command.Utterances) ?? [],
                Enabled = command.Enabled
            });
        }

        return clones;
    }

    private static FormVoiceProfile? CloneFormVoiceProfile(FormVoiceProfile? profile)
    {
        if (profile is null)
        {
            return null;
        }

        return new FormVoiceProfile
        {
            SchemaVersion = profile.SchemaVersion,
            ProfileVersion = profile.ProfileVersion,
            FormVoiceProfileId = profile.FormVoiceProfileId,
            AllowBargeIn = profile.AllowBargeIn,
            NoInputTimeoutMs = profile.NoInputTimeoutMs,
            NoInputMaxRetries = profile.NoInputMaxRetries,
            SttMinConfidenceToAccept = profile.SttMinConfidenceToAccept,
            SttMinConfidenceToConfirm = profile.SttMinConfidenceToConfirm,
            SpeechRate = profile.SpeechRate,
            DiacriticsStrategy = profile.DiacriticsStrategy,
            PauseAfterPromptMs = profile.PauseAfterPromptMs
        };
    }
}
