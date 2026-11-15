using System.Text.Json;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Variablenreferenzen beim Setzen, Löschen und Kopieren
/// sowie unzulässige explizite Nullwerte beim Setzen.
/// </summary>
internal sealed class FormRuleVariableActionValidator
{
    public void ValidateSetValue(JsonElement action, string path, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        var variableId = PreflightJsonValueReader.TryGetString(action, "variableId");
        ValidateVariableReference(variableId, $"{path}.variableId", "setValue references missing variableId.", index, errors);

        if (action.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Null)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorNullForbidden,
                Message: "setValue.value must not be null.",
                Path: $"{path}.value",
                Id: variableId));
        }
    }

    public void ValidateClearValue(JsonElement action, string path, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        var variableId = PreflightJsonValueReader.TryGetString(action, "variableId");
        ValidateVariableReference(variableId, $"{path}.variableId", "clearValue references missing variableId.", index, errors);
    }

    public void ValidateCopyValue(JsonElement action, string path, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        var from = PreflightJsonValueReader.TryGetString(action, "fromVariableId");
        var to = PreflightJsonValueReader.TryGetString(action, "toVariableId");

        ValidateVariableReference(from, $"{path}.fromVariableId", "copyValue references missing fromVariableId.", index, errors);
        ValidateVariableReference(to, $"{path}.toVariableId", "copyValue references missing toVariableId.", index, errors);
    }

    private static void ValidateVariableReference(
        string? variableId,
        string path,
        string message,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(variableId) || !index.VariablesById.ContainsKey(variableId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: message,
                Path: path,
                Id: variableId));
        }
    }
}
