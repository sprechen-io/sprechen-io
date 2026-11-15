using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class SwitchPolitenessThenEchoVoiceCommandDialog(
    IUserLanguageState userLanguageState,
    ITextToSpeechPlayer textToSpeechPlayer,
    PolitenessLevel targetPolitenessLevel,
    ImmutableDictionary<string, string> preservedValuesByVariableId) : IVoiceCommandDialog
{
    private readonly IUserLanguageState _userLanguageState = userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));
    private readonly ITextToSpeechPlayer _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
    private readonly PolitenessLevel _targetPolitenessLevel = targetPolitenessLevel;
    private readonly ImmutableDictionary<string, string> _preservedValuesByVariableId = preservedValuesByVariableId
        ?? throw new ArgumentNullException(nameof(preservedValuesByVariableId));
    private readonly List<(string LanguageCode, ImmutableDictionary<string, string> InitialValuesByVariableId)> _executions = [];
    private int _executionCount;

    public IReadOnlyList<(string LanguageCode, ImmutableDictionary<string, string> InitialValuesByVariableId)> Executions => _executions;

    public async Task<VoiceCommandDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        var normalizedLanguageCode = IsoCode.TryNormalizeIso6391(languageCode)
            ?? throw new InvalidOperationException("Expected normalized ISO 639-1 language code.");
        var valuesSnapshot = initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);

        _executions.Add((normalizedLanguageCode, valuesSnapshot));

        if (_executionCount++ == 0)
        {
            return VoiceCommandDialogResult.ChangePoliteness(
                applicationId,
                formId,
                _targetPolitenessLevel,
                _preservedValuesByVariableId);
        }

        var selectedPolitenessLevel = _userLanguageState.SelectedPolitenessLevel ?? PolitenessLevel.Formal;
        var politenessLabel = selectedPolitenessLevel == PolitenessLevel.Informal
            ? VoiceInterfaceModelCatalog.Lexicon.Politeness.GetInformalWord(normalizedLanguageCode)
            : VoiceInterfaceModelCatalog.Lexicon.Politeness.GetFormalWord(normalizedLanguageCode);

        await _textToSpeechPlayer.SpeakAsync(politenessLabel, normalizedLanguageCode, cancellationToken).ConfigureAwait(false);

        return VoiceCommandDialogResult.Abandoned(applicationId, formId, valuesSnapshot);
    }
}
