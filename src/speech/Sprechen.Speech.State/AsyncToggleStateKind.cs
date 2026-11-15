namespace Sprechen.Speech.State;

/// <summary>
/// Kennzeichnet laufende Schaltvorgänge getrennt von stabilen Zuständen, um konkurrierende Startversuche abzuweisen.
/// </summary>
internal enum AsyncToggleStateKind
{
    Off = 0,
    TurningOn = 1,
    On = 2,
    TurningOff = 3
}


