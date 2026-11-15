namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Hält den nach einem einzelnen Aufruf des Buchstabierhandlers tatsächlich
/// übernommenen Feldwert für SpellingSecretFieldPrivacyEndToEndTests.
/// Ohne Wertübernahme ist er null.
/// </summary>
internal sealed record DriveResult(string? CommittedValue);
