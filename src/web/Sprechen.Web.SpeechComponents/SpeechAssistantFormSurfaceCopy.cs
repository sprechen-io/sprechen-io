namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Bündelt die Oberflächentexte für Leerzustand, Sprache, Felder, Gruppen und Formularstatus.
/// </summary>
public sealed class SpeechAssistantFormSurfaceCopy
{
    public SpeechAssistantFormSurfaceCopy()
        : this(new SpeechAssistantFormSurfaceCopyResolver())
    {
    }

    private SpeechAssistantFormSurfaceCopy(SpeechAssistantFormSurfaceCopyResolver resolver)
    {
        EmptyState = new SpeechAssistantFormSurfaceEmptyStateCopy(resolver);
        Language = new SpeechAssistantFormSurfaceLanguageCopy(resolver);
        Fields = new SpeechAssistantFormSurfaceFieldCopy(resolver);
        FieldGroups = new SpeechAssistantFormSurfaceFieldGroupCopy(resolver);
        Stages = new SpeechAssistantFormSurfaceStageCopy(resolver);
    }

    public SpeechAssistantFormSurfaceEmptyStateCopy EmptyState { get; }

    public SpeechAssistantFormSurfaceLanguageCopy Language { get; }

    public SpeechAssistantFormSurfaceFieldCopy Fields { get; }

    public SpeechAssistantFormSurfaceFieldGroupCopy FieldGroups { get; }

    public SpeechAssistantFormSurfaceStageCopy Stages { get; }
}
