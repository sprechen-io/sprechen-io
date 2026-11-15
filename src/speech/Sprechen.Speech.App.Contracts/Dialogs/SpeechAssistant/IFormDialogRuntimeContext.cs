using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Sprache, Formularvertrag und aktuelle Feldwerte für die Ausführung eines Formulardialogs bereit.
/// </summary>
public interface IFormDialogRuntimeContext
{
    string IsoCode { get; }

    FormContract Contract { get; }

    VoiceDialogSettings VoiceSettings { get; }

    ImmutableDictionary<string, string> ValuesByVariableId { get; }
}

