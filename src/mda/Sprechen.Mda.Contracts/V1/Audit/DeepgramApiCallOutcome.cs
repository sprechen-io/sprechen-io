using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Audit;

/// <summary>
/// Abschließender Status eines protokollierten Deepgram-Aufrufs: submitted oder aborted.
/// Ein Abbruchgrund wird separat in <see cref="DeepgramApiCallAuditEntry.ErrorCode"/> geführt.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeepgramApiCallOutcome>))]
public enum DeepgramApiCallOutcome
{
    [JsonStringEnumMemberName("submitted")]
    Submitted,

    [JsonStringEnumMemberName("aborted")]
    Aborted,
}
