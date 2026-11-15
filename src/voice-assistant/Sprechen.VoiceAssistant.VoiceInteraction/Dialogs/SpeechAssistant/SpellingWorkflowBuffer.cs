namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Sammelt bereinigte Buchstabierblöcke und deren gemeldete Erkennungssicherheit.
/// Einzelne Zeichen, der letzte Block oder der gesamte Inhalt können entfernt werden.
/// Der Puffer übernimmt den Wert nicht selbst in ein Formularfeld.
/// </summary>
internal sealed class SpellingWorkflowBuffer
{
    private readonly AccumulatedValueConfidence _accumulatedValueConfidence = new();
    private readonly List<string> _blocks = [];
    private readonly List<int?> _blockConfidences = [];

    public bool HasCharacters => _blocks.Count > 0;

    // Die aktuelle Blockanzahl für Diagnosen bereitstellen.
    public int BlockCount => _blocks.Count;

    public void Append(string parsedCharacters, int? confidencePercent)
    {
        ArgumentNullException.ThrowIfNull(parsedCharacters);
        var trimmed = parsedCharacters.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        _blocks.Add(trimmed);
        _blockConfidences.Add(confidencePercent);
    }

    public string GetValue() => string.Concat(_blocks);

    /// <summary>
    /// Liefert den letzten verbliebenen Block für die modusspezifische Wiedergabe.
    /// </summary>
    public string GetLastBlock() => _blocks.Count == 0 ? string.Empty : _blocks[^1];

    public string GetSpokenCharacters()
        => string.Join(' ', GetValue().Select(character => character.ToString()));

    /// <summary>
    /// Liefert die niedrigste gemeldete Sicherheit der verbliebenen Blöcke oder
    /// null ohne gemeldete Sicherheit. Die weitere Vertrauensbehandlung liegt beim Aufrufer.
    /// </summary>
    public int? GetSurvivingMinimumConfidencePercent()
        => _accumulatedValueConfidence.SurvivingMinimumPercent(_blockConfidences);

    public bool TryRemoveLastCharacter()
    {
        if (_blocks.Count == 0)
        {
            return false;
        }

        var lastBlock = _blocks[^1];
        if (lastBlock.Length <= 1)
        {
            _blocks.RemoveAt(_blocks.Count - 1);
            _blockConfidences.RemoveAt(_blockConfidences.Count - 1);
            return true;
        }

        // Ein verkürzter, nicht leerer Block behält die Sicherheit seines ursprünglichen Erfassungsschritts.
        _blocks[^1] = lastBlock[..^1];
        return true;
    }

    public bool TryRemoveLastBlock()
    {
        if (_blocks.Count == 0)
        {
            return false;
        }

        _blocks.RemoveAt(_blocks.Count - 1);
        _blockConfidences.RemoveAt(_blockConfidences.Count - 1);
        return true;
    }

    public bool TryClear()
    {
        if (_blocks.Count == 0)
        {
            return false;
        }

        _blocks.Clear();
        _blockConfidences.Clear();
        return true;
    }
}
