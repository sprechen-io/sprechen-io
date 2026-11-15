using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft ein im Formular eingebettetes Sprachdialogprofil
/// und ergänzt dessen Befundpfade um den Formularpfad.
/// </summary>
internal sealed class FormVoiceProfileValidator
{
    public void Validate(FormContract contract, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(errors);
        ValidateVoiceHintValues(contract, errors);
    }
    private static void ValidateVoiceHintValues(FormContract contract, List<PreflightIssue> errors)
    {
        var formVoiceProfile = contract.Projections?.Voice?.FormVoiceProfile;

        if (formVoiceProfile is not null)
        {
            foreach (var issue in FormVoiceProfilePreflight.Validate(formVoiceProfile).Errors)
            {
                errors.Add(new PreflightIssue(
                    Code: issue.Code,
                    Message: issue.Message,
                    Path: PrefixFormVoiceProfilePath(issue.Path),
                    Id: issue.Id));
            }
        }
    }

    private static string PrefixFormVoiceProfilePath(string? relativePath)
    {
        const string prefix = "$.projections.voice.formVoiceProfile";

        if (string.IsNullOrWhiteSpace(relativePath) || string.Equals(relativePath, "$", StringComparison.Ordinal))
        {
            return prefix;
        }

        return relativePath.StartsWith("$.", StringComparison.Ordinal)
            ? $"{prefix}{relativePath[1..]}"
            : $"{prefix}.{relativePath}";
    }

}
