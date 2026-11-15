namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Erfasstes Audio mit MIME-Typ, Chunks und optionaler Kennung des Spracheingabeversuchs.
/// Die Kennung verbindet Erfassungsbericht und Erkennungsanfrage mit dem dienstseitigen Datensatz.
/// Bei Audio ohne diese Erfassung kann die Kennung fehlen.
/// </summary>
public sealed record CapturedSpeechAudio(
    SpeechAudioMimeType MimeType,
    IReadOnlyList<byte[]> Chunks,
    string? TurnId = null);


