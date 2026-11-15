using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

internal sealed class TestStudyTelemetryContext(string? participantId) : IStudyTelemetryContext
{
    public string? ParticipantId { get; private set; } = participantId;

    public void SetStudyContext(string? nextParticipantId)
    {
        ParticipantId = nextParticipantId;
    }
}
