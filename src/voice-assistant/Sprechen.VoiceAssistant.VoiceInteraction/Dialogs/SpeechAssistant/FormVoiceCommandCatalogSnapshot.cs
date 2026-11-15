using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Katalog, ausgewählte Anwendung, Formular und übergebene Spracheinstellungen für den weiteren Kontextaufbau zusammen.
/// </summary>
internal sealed class FormVoiceCommandCatalogSnapshot
{
    public FormVoiceCommandCatalogSnapshot(
        FormsCatalog catalog,
        ApplicationDescriptor application,
        FormDescriptor selectedForm,
        VoiceDialogSettings baseVoiceSettings)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Application = application ?? throw new ArgumentNullException(nameof(application));
        SelectedForm = selectedForm ?? throw new ArgumentNullException(nameof(selectedForm));
        BaseVoiceSettings = baseVoiceSettings ?? throw new ArgumentNullException(nameof(baseVoiceSettings));
    }

    public FormsCatalog Catalog { get; }

    public ApplicationDescriptor Application { get; }

    public FormDescriptor SelectedForm { get; }

    public VoiceDialogSettings BaseVoiceSettings { get; }
}

