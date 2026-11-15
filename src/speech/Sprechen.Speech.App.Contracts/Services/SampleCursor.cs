namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Position im laufenden PCM-Strom eines <c>SpeechSampleRing</c>.
/// Trotz des Namens ist <see cref="SampleIndex"/> ein 64-Bit-Byteoffset.
/// Verbraucher addieren zum Vorrücken die von <c>SpeechSampleRing.Read</c> gelieferte Bytezahl.
/// </summary>
/// <remarks>
/// Der Cursor ist ein Wert des Aufrufers und wird nicht im Ring gespeichert.
/// Liegt er vor den noch verfügbaren Daten, wirft das nächste Lesen
/// <see cref="RingCursorStaleException"/>.
/// </remarks>
public readonly record struct SampleCursor(long SampleIndex);
