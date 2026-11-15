using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Erfasst Ansagekennungen des Formulars und meldet doppelt verwendete Kennungen.
/// </summary>
internal sealed class FormVoicePromptIdentityValidator
{
    public void Validate(FormContract contract, HashSet<string> promptsById, List<PreflightIssue> errors)
    {
        var promptList = contract.Projections?.Voice?.Hints?.Prompts;
        if (promptList is null)
        {
            return;
        }

        for (var i = 0; i < promptList.Count; i++)
        {
            var prompt = promptList[i];
            if (!promptsById.Add(prompt.PromptId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate promptId '{prompt.PromptId}'.",
                    Path: $"$.projections.voice.hints.prompts[{i}].promptId",
                    Id: prompt.PromptId));
            }
        }
    }
}
