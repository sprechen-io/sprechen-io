using Sprechen.Forms.Services;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Übersetzt Ladeausnahmen in stabile Fehlercodes,
/// aus denen die Auswahlseite lokalisierte Meldungen bestimmt.
/// </summary>
internal sealed class IndexPageSelectedFormLoadErrorMapper
{
    public IndexPageSelectedFormResult Map(Exception exception)
    {
        return exception switch
        {
            FormContractLoadFailedException loadFailedException => MapPreflightFailure(loadFailedException),
            InvalidOperationException invalidOperationException
                when IsUnsupportedHtmlProjectionProfile(invalidOperationException) => UnsupportedHtmlProjectionProfile(),
            _ => GenericLoadFailure()
        };
    }

    private IndexPageSelectedFormResult MapPreflightFailure(FormContractLoadFailedException exception)
    {
        return HasLocalizationStrictMissing(exception)
            ? new IndexPageSelectedFormResult
            {
                ErrorCode = "E_LOCALIZATION_STRICT_MISSING"
            }
            : new IndexPageSelectedFormResult
            {
                ErrorCode = "FORM_CONTRACT_PREFLIGHT_FAILED"
            };
    }

    private bool HasLocalizationStrictMissing(FormContractLoadFailedException exception)
    {
        return exception.Issues.Any(issue => issue.Code == PreflightCodes.ErrorLocalizationMissing);
    }

    private bool IsUnsupportedHtmlProjectionProfile(InvalidOperationException exception)
    {
        return exception.Message.StartsWith(HtmlProjectionProfileResolver.ErrorCodeUnsupportedProfile, StringComparison.Ordinal);
    }

    private IndexPageSelectedFormResult UnsupportedHtmlProjectionProfile()
    {
        return new IndexPageSelectedFormResult
        {
            ErrorCode = HtmlProjectionProfileResolver.ErrorCodeUnsupportedProfile
        };
    }

    private IndexPageSelectedFormResult GenericLoadFailure()
    {
        return new IndexPageSelectedFormResult
        {
            ErrorCode = "FORM_CONTRACT_LOAD_FAILED"
        };
    }
}
