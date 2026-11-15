using System.Collections.Immutable;
using System.Globalization;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt einen vorlesbaren Werttext mit Inhaltsstatus aus einem Formularwert.
/// Die Aufrufer bestimmen die Vorlesefreigabe. Satzbildung, Speicherung und Wertübernahme erfolgen außerhalb.
/// </summary>
public sealed class FormSpokenValueProjector
{
    private readonly string _isoCode;

    public FormSpokenValueProjector(string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        _isoCode = isoCode.Trim();
    }

    public FormSpokenValueProjection Project(
        string? canonicalValue,
        string valueTypeKind,
        FormSpokenValueSpeakDisposition disposition,
        ImmutableArray<FormChoiceOption> options,
        string? maskedWording)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valueTypeKind);

        if (disposition == FormSpokenValueSpeakDisposition.Withhold)
        {
            return new FormSpokenValueProjection(
                FormSpokenValueProjectionStatus.Withheld,
                SpokenValue: null,
                ValueResolvedFromOption: false);
        }

        var isSet = string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase);
        var trimmed = canonicalValue?.Trim() ?? string.Empty;
        var isEmpty = trimmed.Length == 0;

        if (isEmpty && isSet)
        {
            if (disposition == FormSpokenValueSpeakDisposition.Mask)
            {
                return new FormSpokenValueProjection(
                    FormSpokenValueProjectionStatus.Masked,
                    SpokenValue: string.Empty,
                    ValueResolvedFromOption: false);
            }

            return ProjectEmptySetWord();
        }

        if (isEmpty)
        {
            return new FormSpokenValueProjection(
                FormSpokenValueProjectionStatus.Empty,
                SpokenValue: string.Empty,
                ValueResolvedFromOption: false);
        }

        if (disposition == FormSpokenValueSpeakDisposition.Mask)
        {
            // Die Maskenformulierung vom Aufrufer übernehmen. Ohne Maske keinen kanonischen
            // Wert einsetzen, da dies die vorgegebene Maskierung umgehen würde.
            var mask = string.IsNullOrWhiteSpace(maskedWording) ? string.Empty : maskedWording.Trim();
            return new FormSpokenValueProjection(
                FormSpokenValueProjectionStatus.Masked,
                mask,
                ValueResolvedFromOption: false);
        }

        return ProjectFullValue(trimmed, valueTypeKind, options);
    }

    private FormSpokenValueProjection ProjectEmptySetWord()
    {
        if (VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
                FormDialogPromptIds.SetEmptySelectionValue,
                _isoCode,
                out var emptyWord)
            && !string.IsNullOrWhiteSpace(emptyWord))
        {
            return new FormSpokenValueProjection(
                FormSpokenValueProjectionStatus.Empty,
                emptyWord.Trim(),
                ValueResolvedFromOption: false);
        }

        return ProjectUnavailable();
    }

    private FormSpokenValueProjection ProjectUnavailable()
    {
        var word = VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
                FormDialogPromptIds.SpokenValueUnavailable,
                _isoCode,
                out var catalogWord)
            && !string.IsNullOrWhiteSpace(catalogWord)
            ? catalogWord.Trim()
            : string.Empty;
        return new FormSpokenValueProjection(
            FormSpokenValueProjectionStatus.Unavailable,
            word,
            ValueResolvedFromOption: false);
    }

    private FormSpokenValueProjection ProjectFullValue(
        string canonical,
        string valueTypeKind,
        ImmutableArray<FormChoiceOption> options)
    {
        var isChoice = string.Equals(valueTypeKind, ValueTypeKinds.Enum, StringComparison.OrdinalIgnoreCase)
            || string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase);
        if (isChoice
            && !options.IsDefaultOrEmpty
            && !FormChoiceOptionDisplayResolver.AllCodesResolveToOptions(options, canonical))
        {
            return ProjectUnavailable();
        }

        var valueResolvedFromOption = FormChoiceOptionDisplayResolver.AllCodesResolveToOptions(options, canonical);
        var display = FormChoiceOptionDisplayResolver.ResolveDisplayValue(options, canonical);

        if (string.Equals(valueTypeKind, ValueTypeKinds.Boolean, StringComparison.OrdinalIgnoreCase))
        {
            if (!bool.TryParse(display, out var isYes))
            {
                return ProjectUnavailable();
            }

            var spoken = isYes ? YesNo.GetYesWord(_isoCode) : YesNo.GetNoWord(_isoCode);
            return new FormSpokenValueProjection(
                FormSpokenValueProjectionStatus.FullValue,
                spoken,
                valueResolvedFromOption);
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Date, StringComparison.OrdinalIgnoreCase)
            && !DateOnly.TryParseExact(
                canonical,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return ProjectUnavailable();
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Time, StringComparison.OrdinalIgnoreCase)
            && !TimeOnly.TryParseExact(
                canonical,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return ProjectUnavailable();
        }

        var spokenValue = FormTemporalSpokenValueResolver.ResolveSpokenValue(valueTypeKind, display, _isoCode);
        return new FormSpokenValueProjection(
            FormSpokenValueProjectionStatus.FullValue,
            spokenValue,
            valueResolvedFromOption);
    }
}
