namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die nichtleeren Textblöcke des Ziffernablaufs samt angegebener
/// Erkennungssicherheit. Löschen und erneutes Diktieren entfernen die betroffenen
/// Blöcke. Ein nur verkürzter Block behält seine Erkennungssicherheit.
/// </summary>
internal sealed class DigitsWorkflowBuffer
{
    private readonly AccumulatedValueConfidence _accumulatedValueConfidence = new();
    private readonly List<string> _digitBlocks = [];
    private readonly List<int?> _blockConfidences = [];

    public bool HasDigits => _digitBlocks.Count > 0;

    public void Append(string digitBlock, int? confidencePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(digitBlock);
        _digitBlocks.Add(digitBlock.Trim());
        _blockConfidences.Add(confidencePercent);
    }

    public string GetDigits()
    {
        return string.Concat(_digitBlocks);
    }

    public string GetSpokenDigits()
    {
        return string.Join(
            ", ",
            _digitBlocks.Select(block =>
                string.Join(" ", block.Trim().Select(character => character.ToString()))));
    }

    /// <summary>
    /// Liefert die kleinste nichtleere Angabe zur Erkennungssicherheit der
    /// verbleibenden Blöcke oder null, wenn keine Angabe verbleibt.
    /// </summary>
    public int? GetSurvivingMinimumConfidencePercent()
        => _accumulatedValueConfidence.SurvivingMinimumPercent(_blockConfidences);

    public bool TryRemoveLastDigit()
    {
        if (_digitBlocks.Count == 0)
        {
            return false;
        }

        var lastBlock = _digitBlocks[^1];
        if (lastBlock.Length <= 1)
        {
            _digitBlocks.RemoveAt(_digitBlocks.Count - 1);
            _blockConfidences.RemoveAt(_blockConfidences.Count - 1);
            return true;
        }

        // Ein verkürzter Block behält die Erkennungssicherheit seiner Aufnahme.
        _digitBlocks[^1] = lastBlock[..^1];
        return true;
    }

    public bool TryRemoveLastBlock()
    {
        if (_digitBlocks.Count == 0)
        {
            return false;
        }

        _digitBlocks.RemoveAt(_digitBlocks.Count - 1);
        _blockConfidences.RemoveAt(_blockConfidences.Count - 1);
        return true;
    }

    public bool TryPrepareRepeatLastBlock()
    {
        if (_digitBlocks.Count == 0)
        {
            return false;
        }

        _digitBlocks.RemoveAt(_digitBlocks.Count - 1);
        _blockConfidences.RemoveAt(_blockConfidences.Count - 1);
        return true;
    }
}
