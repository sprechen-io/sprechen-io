using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt das betrachtete Feld als gemeinsame Grundlage für Ansagen und typgerechte Eingabeverarbeitung bereit.
/// </summary>
public sealed record FormFieldSnapshot(
    FormFieldIdentitySnapshot Identity,
    FormFieldPresentationSnapshot Presentation,
    FormFieldInteractionSnapshot Interaction)
{
    public string FieldId => Identity.FieldId;

    public string VariableId => Identity.VariableId;

    public string FieldGroupId => Identity.FieldGroupId;

    public string FieldGroupTitle => Presentation.FieldGroupTitle;

    public string Label => Presentation.Label;

    public string? HelpText => Presentation.HelpText;

    public ImmutableArray<string> Examples => Presentation.Examples;

    public ImmutableArray<FormChoiceOption> Options => Presentation.Options;

    public bool CodeStyle => Interaction.CodeStyle;

    public InputMode InputMode => Interaction.InputMode;

    public string Editability => Interaction.Editability;

    public string ValueTypeKind => Interaction.ValueTypeKind;

    public bool SpeakValueInConfirmation => Interaction.SpeakValueInConfirmation;

    public int? MinSelected => Interaction.MinSelected;

    public int? MaxSelected => Interaction.MaxSelected;
}


