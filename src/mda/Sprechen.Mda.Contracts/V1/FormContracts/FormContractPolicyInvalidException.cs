namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Meldet nicht auflösbare Datenschutzregeln mit einem typisierten Fehlergrund.
/// Die eigene Fehlernachricht enthält keine Variablenkennungen, Feldwerte oder Beschriftungen.
/// </summary>
public sealed class FormContractPolicyInvalidException : InvalidOperationException
{
    public const string ErrorCode = "FORM_CONTRACT_POLICY_INVALID";

    public FormContractPolicyInvalidException(EffectiveSensitiveDataPolicyFailure failure)
        : base("The form contract contains an invalid privacy policy.")
    {
        if (failure is EffectiveSensitiveDataPolicyFailure.None)
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        Failure = failure;
    }

    public EffectiveSensitiveDataPolicyFailure Failure { get; }
}
