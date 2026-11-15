namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verbindet Aufnahme und Spracherkennung zu einem gemeinsamen Vertrag für Spracheingaben.
/// </summary>
public interface ISpeechInputClient : ISpeechCaptureClient, ISpeechRecognitionClient
{
}
