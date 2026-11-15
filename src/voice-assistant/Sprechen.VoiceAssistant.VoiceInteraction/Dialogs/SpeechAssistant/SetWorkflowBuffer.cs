using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Sammelt unterschiedliche Optionscodes additiv und hält die Sicherheit der
/// Schritte fest, die neue Codes beitragen. Die Ausgabe sortiert die Codes ordinal
/// und verbindet sie mit dem Mengentrenner. Gültige Codes werden vorausgesetzt.
/// </summary>
internal sealed class SetWorkflowBuffer
{
    private readonly AccumulatedValueConfidence _accumulatedValueConfidence = new();
    private readonly List<string> _selectedCodes = [];
    private readonly List<int?> _contributionConfidences = [];

    public bool HasContent => _selectedCodes.Count > 0;

    /// <summary>
    /// Ergänzt bisher nicht ausgewählte Codes und berücksichtigt die übergebene
    /// Sicherheit nur, wenn mindestens ein neuer Code hinzukommt.
    /// </summary>
    public bool TryAdd(IEnumerable<string> codes, int? turnConfidencePercent)
    {
        ArgumentNullException.ThrowIfNull(codes);

        var addedAny = false;
        foreach (var code in codes)
        {
            if (!_selectedCodes.Contains(code, StringComparer.Ordinal))
            {
                _selectedCodes.Add(code);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            _contributionConfidences.Add(turnConfidencePercent);
        }

        return addedAny;
    }

    public string GetFinalizedValue()
        => string.Join("|", _selectedCodes.Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal));

    public string GetSpokenSelection(ImmutableArray<FormChoiceOption> options)
        => string.Join(
            ", ",
            _selectedCodes.Select(code => FormChoiceOptionDisplayResolver.ResolveDisplayName(options, code)));

    /// <summary>
    /// Liefert die niedrigste gemeldete Sicherheit der beitragenden Schritte oder
    /// null, wenn keiner dieser Schritte eine Sicherheit gemeldet hat.
    /// </summary>
    public int? GetSurvivingMinimumConfidencePercent()
        => _accumulatedValueConfidence.SurvivingMinimumPercent(_contributionConfidences);
}
