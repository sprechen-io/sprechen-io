using System.Collections.Immutable;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Eingabemodi gemäß field-definitions.schema.json.
/// </summary>
public static class InputModes
{
    public const string Voice = "voice";
    public const string Keyboard = "keyboard";
    public const string Spelling = "spelling";
    public const string Digits = "digits";
    public const string Literal = "literal";

    private static readonly ImmutableHashSet<string> SupportedModes = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        Voice,
        Keyboard,
        Spelling,
        Digits,
        Literal);

    /// <summary>
    /// Liefert den ersten unterstützten Eingabemodus aus der Vorzugsliste, sonst Voice.
    /// <see cref="InputModeExtensions.ToWireValue"/> liefert den zugehörigen Übertragungswert.
    /// </summary>
    public static InputMode ResolveExpectedInputMode(IReadOnlyList<string>? preferredInputModes)
    {
        if (preferredInputModes is not null)
        {
            foreach (var mode in preferredInputModes)
            {
                if (TryNormalize(mode, out var normalized)
                    && InputModeExtensions.TryFromWireValue(normalized, out var resolved))
                {
                    return resolved;
                }
            }
        }

        return InputMode.Voice;
    }

    /// <summary>
    /// Entfernt Rand-Leerzeichen und prüft den Wert unter Beachtung der Groß-/Kleinschreibung.
    /// Fehlende, leere oder unbekannte Werte ergeben false und eine leere Ausgabe.
    /// </summary>
    public static bool TryNormalize(string? mode, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(mode))
        {
            return false;
        }

        var candidate = mode.Trim();
        if (!SupportedModes.Contains(candidate))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }
}
