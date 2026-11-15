using System.Text.Json;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Ansagereferenz einer Sprechaktion auf eine vorhandene Kennung.
/// </summary>
internal sealed class FormRuleSpeakActionValidator
{
    public void Validate(JsonElement action, string path, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        var promptId = PreflightJsonValueReader.TryGetString(action, "promptId");
        if (string.IsNullOrWhiteSpace(promptId) || !index.PromptIds.Contains(promptId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"speak references missing promptId '{promptId}'.",
                Path: $"{path}.promptId",
                Id: promptId));
        }
    }
}
