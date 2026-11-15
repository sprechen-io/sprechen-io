using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft eine angegebene Mikrofonregel auf die zulässigen Werte
/// <c>heuristic</c> und <c>hardOff</c>. Ein fehlendes Profil ist zulässig.
/// </summary>
internal sealed class SpeechCaptureMicGateValidator
{
    public void Validate(List<PreflightIssue> errors, SpeechCaptureMicGateProfile? micGate)
    {
        if (micGate is null
            || string.Equals(micGate.Policy, "heuristic", StringComparison.Ordinal)
            || string.Equals(micGate.Policy, "hardOff", StringComparison.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"micGate.policy must be one of 'heuristic' or 'hardOff', got '{micGate.Policy}'.",
            Path: "$.micGate.policy"));
    }
}
