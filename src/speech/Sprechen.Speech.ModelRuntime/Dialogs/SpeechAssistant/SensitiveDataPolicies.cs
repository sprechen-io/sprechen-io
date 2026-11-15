using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.ModelRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt maskierte Vorlesetexte für sensible Werte anhand der vom Aufrufer
/// bereits aufgelösten Datenschutzklasse und der modellierten Ansagevorlagen.
/// </summary>
public static class SensitiveDataPolicies
{
    public static string BuildMaskedReadback(string isoCode, string privacyClass, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privacyClass);
        ArgumentNullException.ThrowIfNull(value);

        var trimmedValue = value.Trim();
        if (trimmedValue.Length == 0)
        {
            return ResolvePrompt(isoCode, FormDialogPromptIds.PrivacyReadbackEmpty);
        }

        if (privacyClass == PrivacyClasses.Secret)
        {
            return ResolvePrompt(
                isoCode,
                FormDialogPromptIds.PrivacyReadbackSecretLength,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Length] = trimmedValue.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (privacyClass == PrivacyClasses.Sensitive)
        {
            var suffixLength = trimmedValue.Length >= 4
                ? 4
                : Math.Min(2, trimmedValue.Length);

            var suffix = suffixLength == 0 ? string.Empty : trimmedValue[^suffixLength..];
            return ResolvePrompt(
                isoCode,
                FormDialogPromptIds.PrivacyReadbackSensitiveSuffix,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Suffix] = suffix
                });
        }

        return trimmedValue;
    }

    private static string ResolvePrompt(
        string isoCode,
        string promptId,
        IReadOnlyDictionary<string, string?>? templateParameters = null)
    {
        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(isoCode) ?? VoiceInterfaceModelCatalog.DefaultLanguage;
        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(promptId, normalizedIsoCode, out var template))
        {
            throw new InvalidOperationException($"No privacy readback prompt template registered for '{promptId}' and isoCode '{normalizedIsoCode}'.");
        }

        return PromptTemplateFormatter.Apply(template, templateParameters);
    }
}



