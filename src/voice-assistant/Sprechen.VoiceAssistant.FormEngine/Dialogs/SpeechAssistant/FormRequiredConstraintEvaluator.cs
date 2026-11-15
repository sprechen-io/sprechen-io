using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft eine Pflichtbedingung anhand vorhandener Werte und nicht leerer Mengen, ohne daraus die Herkunft einer Angabe abzuleiten.
/// </summary>
internal sealed class FormRequiredConstraintEvaluator
{
    private readonly IReadOnlyDictionary<string, string> rawValuesByVariableId;

    public FormRequiredConstraintEvaluator(IReadOnlyDictionary<string, string> rawValuesByVariableId)
    {
        this.rawValuesByVariableId = rawValuesByVariableId ?? throw new ArgumentNullException(nameof(rawValuesByVariableId));
    }

    public bool IsSatisfied(string variableId, FormValue value, bool isValueSet, string valueTypeKind)
    {
        if (!isValueSet)
        {
            return false;
        }

        // Eine als Menge geparste Angabe erfüllt die Pflichtbedingung nur mit
        // mindestens einem enthaltenen Wert.
        if (valueTypeKind is ValueTypeKinds.Set)
        {
            return value.Kind == FormValueKind.Set && value.Set.Length > 0;
        }

        // Andere Angaben anhand des vorhandenen, nicht leeren Rohwerts beurteilen.
        // Ein bloß vorhandener Speicherplatz ersetzt keine ausgefüllte Angabe.
        var rawValue = rawValuesByVariableId.TryGetValue(variableId, out var resolvedValue) ? resolvedValue : null;
        return !string.IsNullOrWhiteSpace(rawValue);
    }
}
