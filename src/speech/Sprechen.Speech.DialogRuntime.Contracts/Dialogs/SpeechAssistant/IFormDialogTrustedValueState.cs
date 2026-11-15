using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt interne Variablenwerte für vertrauenswürdige Laufzeitverbraucher bereit. Der Vertrag ist kein Oberflächenabbild.
/// </summary>
public interface IFormDialogTrustedValueState
{
    Task<ImmutableDictionary<string, string>> GetValuesSnapshotAsync();
}
