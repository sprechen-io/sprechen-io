using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft vorhandene Zielobjekte von Sichtbarkeitsaktionen
/// auf zulässige Arten und bekannte Feld- oder Feldgruppenkennungen.
/// </summary>
internal sealed class FormRuleVisibilityActionValidator
{
    public void Validate(JsonElement action, string path, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        if (!action.TryGetProperty("target", out var targetElement) || targetElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var targetKind = PreflightJsonValueReader.TryGetString(targetElement, "kind");
        switch (targetKind)
        {
            case TargetKinds.Field:
                ValidateFieldTarget(targetElement, path, index, errors);
                break;
            case TargetKinds.FieldGroup:
                ValidateFieldGroupTarget(targetElement, path, index, errors);
                break;
            default:
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported visibility target kind '{targetKind ?? "<missing>"}'.",
                    Path: $"{path}.target.kind"));
                break;
        }
    }

    private static void ValidateFieldTarget(
        JsonElement targetElement,
        string path,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors)
    {
        var fieldId = PreflightJsonValueReader.TryGetString(targetElement, "fieldId");
        if (string.IsNullOrWhiteSpace(fieldId) || !index.FieldsById.ContainsKey(fieldId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"setVisible target fieldId '{fieldId}' not found.",
                Path: $"{path}.target.fieldId",
                Id: fieldId));
        }
    }

    private static void ValidateFieldGroupTarget(
        JsonElement targetElement,
        string path,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors)
    {
        var fieldGroupId = PreflightJsonValueReader.TryGetString(targetElement, "fieldGroupId");
        if (string.IsNullOrWhiteSpace(fieldGroupId) || !index.FieldGroupsById.ContainsKey(fieldGroupId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"setVisible target fieldGroupId '{fieldGroupId}' not found.",
                Path: $"{path}.target.fieldGroupId",
                Id: fieldGroupId));
        }
    }
}
