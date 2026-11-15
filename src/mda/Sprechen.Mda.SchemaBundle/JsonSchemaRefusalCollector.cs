using Json.Schema;

namespace Sprechen.Mda.SchemaBundle;

/// <summary>
/// Sammelt und formatiert Befunde einer JSON-Schemaprüfung
/// mit Dokumentposition, Schlüsselwort und Fehlermeldung.
/// </summary>
public sealed class JsonSchemaRefusalCollector
{
    /// <summary>
    /// Schlüsselwörter für noch nicht ausgewertete Mitglieder. Ihre Befunde werden
    /// unterdrückt, wenn andere Regelverletzungen vorhanden sind.
    /// </summary>
    private static readonly string[] LeftoverMemberKeywords = ["unevaluatedProperties", "unevaluatedItems"];

    /// <summary>
    /// Schlüsselwörter für Alternativgruppen, deren Detailbefunde gemeinsam betrachtet werden.
    /// </summary>
    private static readonly string[] AlternativeSetKeywords = ["oneOf", "anyOf"];

    public IReadOnlyList<JsonSchemaRefusal> Collect(EvaluationResults evaluationResult)
    {
        ArgumentNullException.ThrowIfNull(evaluationResult);

        var refusals = new List<JsonSchemaRefusal>();
        CollectRefusals(evaluationResult, refusals);

        // Fehlgeschlagene Zweige können Mitglieder als nicht ausgewertet zurücklassen.
        // Vorrang für andere Regelverletzungen vermeidet dadurch irreführende Zusatzmeldungen.
        return refusals.Any(refusal => !IsLeftoverMemberRefusal(refusal))
            ? refusals.Where(static refusal => !IsLeftoverMemberRefusal(refusal)).ToArray()
            : refusals;
    }

    public string Describe(EvaluationResults evaluationResult)
    {
        return Describe(Collect(evaluationResult));
    }

    public string Describe(IReadOnlyList<JsonSchemaRefusal> refusals)
    {
        ArgumentNullException.ThrowIfNull(refusals);

        return string.Join(
            "; ",
            refusals.Select(refusal => $"{refusal.InstanceLocation} {refusal.Keyword}: {refusal.Message}"));
    }

    private static bool IsLeftoverMemberRefusal(JsonSchemaRefusal refusal)
    {
        return LeftoverMemberKeywords.Contains(refusal.Keyword, StringComparer.Ordinal);
    }

    private static void CollectRefusals(EvaluationResults evaluationResult, ICollection<JsonSchemaRefusal> refusals)
    {
        if (evaluationResult.IsValid)
        {
            // Auch gültige Knoten können Fehlerdetails ungenutzter Zweige enthalten.
            // Diese Details sind keine Verletzungen des geprüften Dokuments.
            return;
        }

        var instanceLocation = ResolveInstanceLocation(evaluationResult);
        foreach (var error in evaluationResult.Errors ?? [])
        {
            refusals.Add(new JsonSchemaRefusal(
                instanceLocation,
                ResolveKeyword(error.Key, evaluationResult),
                error.Value));
        }

        foreach (var alternativeSet in GroupByAlternativeSet(evaluationResult.Details ?? []))
        {
            if (alternativeSet.Any(static alternative => alternative.IsValid))
            {
                // Bei einer erfüllten Alternative werden die Fehler anderer Alternativen
                // nicht als zusätzliche Detailbefunde ausgegeben. Fehler der Alternativgruppe
                // selbst bleiben erhalten, etwa mehrere Treffer bei oneOf.
                continue;
            }

            foreach (var alternative in alternativeSet)
            {
                if (IsFailedIfCondition(alternative))
                {
                    // Ein nicht erfülltes if bestimmt die Auswahl des else-Zweigs.
                    // Der Fehlschlag der Bedingung ist selbst keine Dokumentverletzung.
                    continue;
                }

                CollectRefusals(alternative, refusals);
            }
        }
    }

    private static IEnumerable<IReadOnlyList<EvaluationResults>> GroupByAlternativeSet(
        IEnumerable<EvaluationResults> details)
    {
        return details
            .GroupBy(ResolveAlternativeSetPath, StringComparer.Ordinal)
            .Select(static group => (IReadOnlyList<EvaluationResults>)[.. group]);
    }

    private static string ResolveAlternativeSetPath(EvaluationResults detail)
    {
        var evaluationPath = detail.EvaluationPath.ToString();
        var lastSeparator = evaluationPath.LastIndexOf('/');
        if (lastSeparator <= 0 || !int.TryParse(evaluationPath[(lastSeparator + 1)..], out _))
        {
            return evaluationPath;
        }

        var setPath = evaluationPath[..lastSeparator];
        var setSeparator = setPath.LastIndexOf('/');
        var setKeyword = setSeparator < 0 ? setPath : setPath[(setSeparator + 1)..];
        return AlternativeSetKeywords.Contains(setKeyword, StringComparer.Ordinal)
            ? setPath
            : evaluationPath;
    }

    private static bool IsFailedIfCondition(EvaluationResults detail)
    {
        if (detail.IsValid)
        {
            return false;
        }

        var evaluationPath = detail.EvaluationPath.ToString();
        var lastSeparator = evaluationPath.LastIndexOf('/');
        var keyword = lastSeparator < 0 ? evaluationPath : evaluationPath[(lastSeparator + 1)..];
        return string.Equals(keyword, "if", StringComparison.Ordinal);
    }

    private static string ResolveKeyword(string errorKey, EvaluationResults evaluationResult)
    {
        if (!string.IsNullOrEmpty(errorKey))
        {
            return errorKey;
        }

        // Ein false-Unterschema kann ohne Schlüsselwort ablehnen.
        // In diesem Fall wird das letzte Segment des Auswertungspfads verwendet.
        var evaluationPath = evaluationResult.EvaluationPath.ToString();
        var lastSeparator = evaluationPath.LastIndexOf('/');
        return lastSeparator < 0
            ? evaluationPath
            : evaluationPath[(lastSeparator + 1)..];
    }

    private static string ResolveInstanceLocation(EvaluationResults evaluationResult)
    {
        var instanceLocationText = evaluationResult.InstanceLocation.ToString();
        return string.IsNullOrWhiteSpace(instanceLocationText)
            ? "$"
            : instanceLocationText;
    }
}
