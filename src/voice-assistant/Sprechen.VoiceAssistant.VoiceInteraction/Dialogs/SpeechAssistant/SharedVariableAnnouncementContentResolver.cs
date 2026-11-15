using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Variablenbezeichnungen und Quellenangaben zu den Inhalten der Ansage über gemeinsam verwendete Werte.
/// </summary>
internal sealed class SharedVariableAnnouncementContentResolver
{
    private readonly SharedVariableAnnouncementLabelResolver _labelResolver;
    private readonly SharedVariableAnnouncementSourceResolver _sourceResolver;

    public SharedVariableAnnouncementContentResolver(
        SharedVariableAnnouncementLabelResolver labelResolver,
        SharedVariableAnnouncementSourceResolver sourceResolver)
    {
        _labelResolver = labelResolver ?? throw new ArgumentNullException(nameof(labelResolver));
        _sourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
    }

    public SharedVariableAnnouncementContent Resolve(
        ApplicationDescriptor application,
        FormContract contract,
        ImmutableArray<(string VariableId, string? SourceFormId)> adoptions,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        var labels = _labelResolver.ResolveLabels(contract, adoptions, isoCode, voiceSettings);
        return new SharedVariableAnnouncementContent(
            _sourceResolver.ResolveSourceName(application, adoptions, isoCode, voiceSettings),
            FormatLabelText(labels));
    }

    private static string FormatLabelText(string[] labels)
        => labels.Length switch
        {
            0 => string.Empty,
            1 => labels[0].Trim(),
            _ => string.Join(", ", labels.Select(label => label.Trim()))
        };
}
