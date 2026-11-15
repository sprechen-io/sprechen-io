using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Audit;

/// <summary>
/// Unterscheidet Audit-Einträge für Deepgram-Spracherkennung, Sprachsynthese und Gespräche.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AuditEntryKind>))]
public enum AuditEntryKind
{
    [JsonStringEnumMemberName("deepgram-stt")]
    DeepgramSpeechToTextCall,

    [JsonStringEnumMemberName("deepgram-tts")]
    DeepgramTextToSpeechCall,

    [JsonStringEnumMemberName("deepgram-conversation")]
    DeepgramConversationCall
}
