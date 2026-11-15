using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus.GetFormStatusQueryHandlerTestsSupport;

internal sealed class TestFormState : IReadableFormStatus
{
    public TestFormState(
        string? currentFieldId,
        string? currentFieldGroupId,
        int totalFields,
        int totalFieldGroups,
        int openRequiredCount,
        int validationErrorCount,
        int currentFieldIndex = 0,
        int currentFieldGroupIndex = 0)
    {
        CurrentFieldId = currentFieldId;
        CurrentFieldGroupId = currentFieldGroupId;
        TotalFields = totalFields;
        TotalFieldGroups = totalFieldGroups;
        OpenRequiredCount = openRequiredCount;
        ValidationErrorCount = validationErrorCount;
        CurrentFieldIndex = currentFieldIndex;
        CurrentFieldGroupIndex = currentFieldGroupIndex;
    }

    public string? CurrentFieldId { get; }

    public string? CurrentFieldGroupId { get; }

    public int TotalFields { get; }

    public int TotalFieldGroups { get; }

    public int OpenRequiredCount { get; }

    public int ValidationErrorCount { get; }

    public int CurrentFieldIndex { get; }

    public int CurrentFieldGroupIndex { get; }
}


