using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Art einer Regelaktion und delegiert die Prüfung ihrer Referenzen
/// an den passenden Aktionsvalidator.
/// </summary>
internal sealed class FormRuleActionValidator
{
    private static readonly IReadOnlySet<string> SupportedActionKinds = ReferenceImplementationSupportProfile.SupportedActionKinds;
    private readonly FormRuleVisibilityActionValidator _visibilityActionValidator = new();
    private readonly FormRuleFieldActionValidator _fieldActionValidator = new();
    private readonly FormRuleFieldGroupActionValidator _fieldGroupActionValidator = new();
    private readonly FormRuleVariableActionValidator _variableActionValidator = new();
    private readonly FormRuleSpeakActionValidator _speakActionValidator = new();

    internal void Validate(JsonElement action, string path, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(errors);

        if (action.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: "Rule action must be a JSON object.",
                Path: path));
            return;
        }

        var kind = PreflightJsonValueReader.TryGetString(action, "kind");
        if (kind is null || !SupportedActionKinds.Contains(kind))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: $"Unsupported action kind '{kind ?? "<missing>"}'.",
                Path: $"{path}.kind"));
            return;
        }

        ValidateSupportedAction(action, path, kind, index, errors);
    }

    private void ValidateSupportedAction(
        JsonElement action,
        string path,
        string kind,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors)
    {
        switch (kind)
        {
            case ActionKinds.SetVisible:
                _visibilityActionValidator.Validate(action, path, index, errors);
                break;
            case ActionKinds.SetEnabled:
            case ActionKinds.SetReadOnly:
            case ActionKinds.FocusField:
                _fieldActionValidator.Validate(action, path, kind, index, errors);
                break;
            case ActionKinds.NavigateToFieldGroup:
                _fieldGroupActionValidator.Validate(action, path, index, errors);
                break;
            case ActionKinds.SetValue:
                _variableActionValidator.ValidateSetValue(action, path, index, errors);
                break;
            case ActionKinds.ClearValue:
                _variableActionValidator.ValidateClearValue(action, path, index, errors);
                break;
            case ActionKinds.CopyValue:
                _variableActionValidator.ValidateCopyValue(action, path, index, errors);
                break;
            case ActionKinds.Speak:
                _speakActionValidator.Validate(action, path, index, errors);
                break;
        }
    }
}
