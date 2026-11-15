namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Vereinheitlicht Eingabe und Darstellung benutzerdefinierter Aktivierungsformen, damit leere oder doppelte Einträge nicht gespeichert werden.
/// </summary>
public sealed class WakeWordActivationFormCodec
{
    public string[] Normalize(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    public string[] Parse(string? transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return [];
        }

        var parts = transcript.Split(
            [',', ';', '\n', '\r', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Normalize(parts);
    }

    public string Format(IEnumerable<string> activationForms)
    {
        ArgumentNullException.ThrowIfNull(activationForms);
        return string.Join(", ", Normalize(activationForms));
    }
}
