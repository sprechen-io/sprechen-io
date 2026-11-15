namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Trennt das längste passende Abschlusskommando vom Ende einer Äußerung,
/// wenn davor mindestens ein Wort Inhalt bleibt. Jeder mögliche Suffix wird
/// mit derselben Regel wie ein isoliertes Abschlusskommando geprüft.
/// So kann etwa eine gemeinsam erkannte Werteingabe mit Abschlusswort
/// vor dem Abschließen noch übernommen werden.
/// </summary>
internal static class CharacterAccumulationFinalizeSuffix
{
    public static bool TryStrip(
        CharacterAccumulationContext context,
        string transcript,
        string finalizeCommandId,
        out string remainderBeforeFinalize)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(finalizeCommandId);

        remainderBeforeFinalize = string.Empty;
        var tokens = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var suffixTokenCount = tokens.Length - 1; suffixTokenCount >= 1; suffixTokenCount--)
        {
            var suffix = string.Join(' ', tokens[^suffixTokenCount..]);
            if (FormGlobalCommandParser.IsIsolatedCommandSignal(
                    context.Context.Contract,
                    suffix,
                    finalizeCommandId,
                    context.Context.IsoCode,
                    context.Context.VoiceSettings.GetPolitenessLevel()))
            {
                remainderBeforeFinalize = string.Join(' ', tokens[..^suffixTokenCount]);
                return remainderBeforeFinalize.Length > 0;
            }
        }

        return false;
    }
}
