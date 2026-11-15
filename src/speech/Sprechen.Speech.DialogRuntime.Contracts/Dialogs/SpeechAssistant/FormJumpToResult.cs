using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis eines Sprungkommandos mit Fokuswechsel oder Vorlesedaten für ein
/// schreibgeschütztes Ziel. Ein einzelnes Feld liefert einen Eintrag. Eine
/// schreibgeschützte Feldgruppe liefert ihren lokalisierten Titel und die zugehörigen Einträge.
/// </summary>
public sealed record FormJumpToResult
{
    private FormJumpToResult(FormJumpToResultKind kind, string? fieldGroupTitle, ImmutableArray<FormReadOnlyFieldReadback> readbacks)
    {
        Kind = kind;
        FieldGroupTitle = fieldGroupTitle;
        Readbacks = readbacks;
    }

    public FormJumpToResultKind Kind { get; }

    /// <summary>Lokalisierter Titel eines schreibgeschützten Gruppenziels, sonst null.</summary>
    public string? FieldGroupTitle { get; }

    /// <summary>Vorlesedaten für <see cref="FormJumpToResultKind.SpokenReadOnlyTarget"/>, sonst leer.</summary>
    public ImmutableArray<FormReadOnlyFieldReadback> Readbacks { get; }

    public static FormJumpToResult Focused()
        => new(FormJumpToResultKind.Focused, fieldGroupTitle: null, []);

    public static FormJumpToResult NotFound()
        => new(FormJumpToResultKind.NotFound, fieldGroupTitle: null, []);

    public static FormJumpToResult ForReadOnlyField(FormReadOnlyFieldReadback readback)
    {
        ArgumentNullException.ThrowIfNull(readback);
        return new(FormJumpToResultKind.SpokenReadOnlyTarget, fieldGroupTitle: null, [readback]);
    }

    public static FormJumpToResult ForReadOnlyFieldGroup(string fieldGroupTitle, ImmutableArray<FormReadOnlyFieldReadback> readbacks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldGroupTitle);
        return new(FormJumpToResultKind.SpokenReadOnlyTarget, fieldGroupTitle, readbacks);
    }
}
