using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Fordert die Übersicht aus Feldgruppen und Feldern an, die der Sprachassistent
/// für die Erkundung der Formularnavigation verwendet.
/// </summary>
internal sealed record GetDiscoverySnapshotQuery : IQuery<FormDiscoverySnapshot>;
