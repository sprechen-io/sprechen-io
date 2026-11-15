using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Bedingungsprüfung, Schweregradordnung und Zielbezug für die Validierung
/// von Formularfeldwerten.
/// </summary>
internal sealed class FormConstraintValidator
{
    private static readonly FormConstraintSeverityRanker SeverityRanker = new();
    private static readonly FormConstraintTargetMatcher TargetMatcher = new();

    private readonly FormConstraintKindDispatcher constraintKindDispatcher;

    public FormConstraintValidator(IReadOnlyDictionary<string, string> rawValuesByVariableId, Func<string, FormValue?> getVariableValue)
    {
        ArgumentNullException.ThrowIfNull(rawValuesByVariableId);
        ArgumentNullException.ThrowIfNull(getVariableValue);

        constraintKindDispatcher = new FormConstraintKindDispatcher(rawValuesByVariableId, getVariableValue);
    }

    /// <summary>
    /// Prüft die angegebene Bedingung über den zuständigen Bedingungsprüfer.
    /// </summary>
    /// <param name="constraint">Die zu prüfende Bedingungsdefinition.</param>
    /// <param name="variableId">Die Kennung der geprüften Variablen.</param>
    /// <param name="value">Der geparste Formularwert.</param>
    /// <param name="isValueSet">Das vom Aufrufer angegebene Kennzeichen eines vorhandenen Werts.</param>
    /// <param name="valueTypeKind">Die Werttypart aus der Variablendefinition.</param>
    /// <returns>Ob die unterstützte Bedingung erfüllt ist.</returns>
    public bool IsConstraintSatisfied(
        ConstraintDefinition constraint,
        string variableId,
        FormValue value,
        bool isValueSet,
        string valueTypeKind)
        => constraintKindDispatcher.IsConstraintSatisfied(constraint, variableId, value, isValueSet, valueTypeKind);

    /// <summary>
    /// Liefert den Rang für die Ordnung von Verstößen: Fehler 3, Warnung 2, Information 1
    /// und unbekannter Schweregrad 0.
    /// </summary>
    public static int SeverityRank(string severity)
        => SeverityRanker.Rank(severity);

    public static bool IsConstraintRelevantToField(ConstraintDefinition constraint, string fieldId, string variableId)
        => TargetMatcher.IsRelevantToField(constraint, fieldId, variableId);
}
