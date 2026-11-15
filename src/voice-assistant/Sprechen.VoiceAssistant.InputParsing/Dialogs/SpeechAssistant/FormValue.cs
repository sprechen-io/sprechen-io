using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt einen geparsten Formularwert mit Wertart, damit boolesche Werte,
/// Ganzzahlen, Zeichenketten und normalisierte Mengen unterscheidbar bleiben.
/// </summary>
public readonly record struct FormValue
{
    public FormValueKind Kind { get; }

    public bool Boolean { get; }

    public int Integer { get; }

    public string String { get; } = string.Empty;

    public ImmutableArray<string> Set { get; } = [];

    private FormValue(FormValueKind kind, bool boolean, int integer, string? @string, ImmutableArray<string> set)
    {
        Kind = kind;
        Boolean = boolean;
        Integer = integer;
        String = @string ?? string.Empty;
        Set = set;
    }

    public static FormValue FromBoolean(bool value)
        => new(FormValueKind.Boolean, value, integer: 0, @string: null, set: []);

    public static FormValue FromInteger(int value)
        => new(FormValueKind.Integer, boolean: false, value, @string: null, set: []);

    public static FormValue FromString(string value)
        => new(FormValueKind.String, boolean: false, integer: 0, value, set: []);

    public static FormValue FromSet(ImmutableArray<string> values)
    {
        var normalized = values
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToImmutableArray();

        return new FormValue(FormValueKind.Set, boolean: false, integer: 0, @string: null, normalized);
    }
}


