using Sprechen.Mda.Contracts.V1.Fields;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Namen und Synonyme der Sprachprojektion wiederverwendbarer Felder
/// auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class ReusableFieldVoiceProjectionLocalizationCoverageValidator
{
    internal static void Validate(
        ReusableFieldDefinition definition,
        string path,
        string context,
        string language,
        string localizationCode,
        List<PreflightIssue> localizationIssues)
    {
        FormLocalizedTextCoverageRule.Validate(
            definition.Projections?.Voice?.Names,
            language,
            $"{path}.projections.voice.names",
            $"{context} voice names",
            localizationIssues,
            definition.FieldDefinitionId,
            localizationCode);

        if (definition.Projections?.Voice?.Synonyms is null)
        {
            return;
        }

        for (var j = 0; j < definition.Projections.Voice.Synonyms.Count; j++)
        {
            FormLocalizedTextCoverageRule.Validate(
                definition.Projections.Voice.Synonyms[j],
                language,
                $"{path}.projections.voice.synonyms[{j}]",
                $"{context} voice synonym",
                localizationIssues,
                definition.FieldDefinitionId,
                localizationCode);
        }
    }
}
