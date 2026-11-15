using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.VoiceCommands;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erschließt formularweite Sprachbefehle und entfernt modellierte Befehlspräfixe für ihre Auswertung.
/// </summary>
public static partial class FormGlobalCommandParser
{
    private static readonly FormGlobalCommandParserOptions DefaultOptions = new();
    private static readonly FormGlobalCommandMatchResolver MatchResolver = new();
    private static readonly FormGlobalCommandLabelResolver LabelResolver = new();
    private static readonly FormGlobalCommandLeadInDetector LeadInDetector = new();
    private static readonly FormGlobalCommandIsolationSignalDetector IsolationSignalDetector = new();

    public static bool TryParse(
        FormContract contract,
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string commandId,
        out string? argument)
    {
        return TryParse(
            contract,
            transcript,
            isoCode,
            politenessLevel,
            DefaultOptions,
            out commandId,
            out argument);
    }

    public static bool TryParse(
        FormContract contract,
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormGlobalCommandParserOptions? options,
        out string commandId,
        out string? argument)
        => MatchResolver.TryParse(
            contract,
            transcript,
            isoCode,
            politenessLevel,
            options ?? DefaultOptions,
            out commandId,
            out argument);

    public static string? ResolveCommandLabel(
        FormContract contract,
        string commandId,
        string isoCode,
        PolitenessLevel politenessLevel)
        => LabelResolver.ResolveCommandLabel(contract, commandId, isoCode, politenessLevel);

    public static bool HasCommandLeadIn(string transcript, IReadOnlyCollection<string> acceptedAnchorForms)
        => LeadInDetector.HasCommandLeadIn(transcript, acceptedAnchorForms);

    public static bool IsBareCommandAnchor(string transcript, IReadOnlyCollection<string> acceptedAnchorForms)
        => LeadInDetector.IsBareCommandAnchor(transcript, acceptedAnchorForms);

    public static bool IsIsolatedCommandSignal(
        FormContract contract,
        string transcript,
        string commandId,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        return IsIsolatedCommandSignal(
            contract,
            transcript,
            commandId,
            isoCode,
            politenessLevel,
            DefaultOptions);
    }

    public static bool IsIsolatedCommandSignal(
        FormContract contract,
        string transcript,
        string commandId,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormGlobalCommandParserOptions options)
        => IsolationSignalDetector.IsIsolatedCommandSignal(
            contract,
            transcript,
            commandId,
            isoCode,
            politenessLevel,
            options);

    public static bool TryStripCommandPrefix(
        FormContract contract,
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string strippedTranscript)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        return FormGlobalCommandPrefixStripper.TryStripPrefix(
            transcript,
            FormVoiceCommandCatalogResolver.ResolvePrefixes(contract, VoiceCommandPrefixKinds.Command),
            isoCode,
            politenessLevel,
            out strippedTranscript);
    }

    public static bool TryStripCommandPrefix(string transcript, string isoCode, out string strippedTranscript)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        return FormGlobalCommandPrefixStripper.TryStripPrefix(
            transcript,
            FormVoiceCommandCatalogResolver.ResolveDefaultPrefixes(VoiceCommandPrefixKinds.Command),
            isoCode,
            PolitenessLevel.Formal,
            out strippedTranscript);
    }

}
