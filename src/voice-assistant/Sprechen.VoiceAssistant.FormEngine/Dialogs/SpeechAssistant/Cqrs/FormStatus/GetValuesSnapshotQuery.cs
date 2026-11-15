using System.Collections.Immutable;
using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Fordert eine unveränderliche Momentaufnahme der gespeicherten Werte effektiv
/// sichtbarer Variablen an.
/// </summary>
internal sealed record GetValuesSnapshotQuery : IQuery<ImmutableDictionary<string, string>>;
