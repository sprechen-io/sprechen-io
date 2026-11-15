namespace Sprechen.Service.Grpc;

/// <summary>
/// Kennzeichnet reguläres Stromende, Ende durch Inaktivität oder einen noch nicht erfassten Abschluss.
/// </summary>
internal enum GrpcConversationAudioInputCompletionReason
{
    None = 0,
    Natural = 1,
    InactivityTimeout = 2
}
