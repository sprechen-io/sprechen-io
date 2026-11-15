namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Hält Ausgangstyp und vollständige Diagnosespur eines Aufnahmeabschlusses,
/// damit die Tests beide Aufnahmewege über ihre gesamte Folge vergleichen können.
/// </summary>
internal sealed record CaptureTerminalObservation(string TerminalKind, IReadOnlyList<string> Entries);
