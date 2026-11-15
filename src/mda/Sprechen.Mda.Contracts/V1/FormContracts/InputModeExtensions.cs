namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Ordnet <see cref="InputMode"/> den Übertragungswerten aus
/// <see cref="InputModes"/> zu, ohne deren Zeichenketten zu duplizieren.
/// </summary>
public static class InputModeExtensions
{
    /// <summary>
    /// Liefert die zugehörige Übertragungskonstante. Nicht definierte Enumwerte
    /// lösen eine ArgumentOutOfRangeException aus.
    /// </summary>
    public static string ToWireValue(this InputMode mode)
    {
        return mode switch
        {
            InputMode.Voice => InputModes.Voice,
            InputMode.Keyboard => InputModes.Keyboard,
            InputMode.Spelling => InputModes.Spelling,
            InputMode.Digits => InputModes.Digits,
            InputMode.Literal => InputModes.Literal,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown input mode.")
        };
    }

    /// <summary>
    /// Ordnet einen exakt passenden Übertragungswert einem Eingabemodus zu.
    /// Bei unbekannten Werten wird false geliefert. Der out-Wert ist dann
    /// der Enumstandard Voice und darf nicht als erfolgreicher Treffer verwendet werden.
    /// </summary>
    public static bool TryFromWireValue(string? wire, out InputMode mode)
    {
        switch (wire)
        {
            case InputModes.Voice:
                mode = InputMode.Voice;
                return true;
            case InputModes.Keyboard:
                mode = InputMode.Keyboard;
                return true;
            case InputModes.Spelling:
                mode = InputMode.Spelling;
                return true;
            case InputModes.Digits:
                mode = InputMode.Digits;
                return true;
            case InputModes.Literal:
                mode = InputMode.Literal;
                return true;
            default:
                mode = default;
                return false;
        }
    }
}
