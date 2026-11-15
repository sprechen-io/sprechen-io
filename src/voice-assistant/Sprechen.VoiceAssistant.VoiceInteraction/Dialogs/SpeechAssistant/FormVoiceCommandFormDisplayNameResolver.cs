using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt den Anzeigenamen eines Formulars aus dem gemeinsamen Katalog
/// anhand von Sprache und Höflichkeitsstufe. Die Formularauswahl verwendet
/// dadurch dieselben Bezeichnungen.
/// </summary>
internal sealed class FormVoiceCommandFormDisplayNameResolver
{
    private readonly FormsCatalogTextResolver _textResolver = new();

    public string Resolve(
        ApplicationDescriptor application,
        FormDescriptor form,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(form);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        return _textResolver.ResolveFormDisplayName(
            application,
            form,
            isoCode,
            voiceSettings.GetPolitenessLevel());
    }
}
