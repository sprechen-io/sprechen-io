namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Ergänzt die Diagnose einer ausgebliebenen Aktivierung um Tokenanzahl und
/// verwendete Aktivierungsformen, damit der Abgleich ohne Transkriptwortlaut auskommt.
/// </summary>
public sealed record WakeWordMissDiagnosticDetail(
    int? TokenCount,
    IReadOnlyList<string>? ActivationForms,
    IReadOnlyList<string>? RecognitionOnlyActivationForms);
