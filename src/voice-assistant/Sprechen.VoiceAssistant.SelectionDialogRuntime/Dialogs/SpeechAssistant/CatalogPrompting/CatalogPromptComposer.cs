using System.Text;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant.CatalogPrompting;

/// <summary>
/// Setzt modellierte Katalogtexte zu einfachen oder gegliederten Auswahlansagen zusammen. Die
/// gemeinsame Auflösung hält Parameterersetzung und Sprachregeln über beide Ausgabeformen hinweg
/// zusammen.
/// </summary>
internal static class CatalogPromptComposer
{
    /// <summary>
    /// Löst eine modellierte Ansage nach den Sprachvorgaben des Kontexts auf. Der strikte
    /// Modus bestimmt, welche Ersatzsprachen zulässig sind. Fehlt ein verwendbarer Text,
    /// wird <see cref="ModeledVoiceResourcePromptException" /> ausgelöst.
    /// </summary>
    public static string ResolvePromptOrThrow(
        CatalogPromptContext promptContext,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?>? templateParameters = null)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (CatalogPromptResolver.TryResolvePrompt(
                promptContext,
                promptId,
                isoCode,
                politenessLevel,
                templateParameters,
                out var resolvedPrompt))
        {
            return resolvedPrompt;
        }

        throw new ModeledVoiceResourcePromptException(
            ModeledVoiceResourcePromptException.MissingLocalizedTextCodeValue,
            $"Selection-dialog prompt '{promptId}' could not be resolved for language '{isoCode}' (politeness '{politenessLevel}') under the dialog's declared localization; refusing a foreign-language fallback.");
    }

    public static string BuildNumberedSequencePrompt<TItem>(
        CatalogNumberedSequencePromptRequest<TItem> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendSegment(
            builder,
            ResolvePromptOrThrow(
                request.PromptContext,
                request.Intro.PromptId,
                request.IsoCode,
                request.PolitenessLevel,
                request.Intro.TemplateParameters));

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            AppendSegment(
                builder,
                ResolvePromptOrThrow(
                    request.PromptContext,
                    request.Item.PromptId,
                    request.IsoCode,
                    request.PolitenessLevel,
                    request.Item.TemplateParametersFactory(i, item)));
        }

        if (request.Trailing is not null)
        {
            AppendSegment(
                builder,
                ResolvePromptOrThrow(
                    request.PromptContext,
                    request.Trailing.PromptId,
                    request.IsoCode,
                    request.PolitenessLevel,
                    request.Trailing.TemplateParameters));
        }

        return builder.ToString().Trim();
    }

    public static SelectionDialogPrompt BuildNumberedSequenceSelectionPrompt<TItem>(
        CatalogNumberedSequencePromptRequest<TItem> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items.Count == 0)
        {
            return SelectionDialogPrompt.FromText(string.Empty);
        }

        var introText = ResolvePromptOrThrow(
            request.PromptContext,
            request.Intro.PromptId,
            request.IsoCode,
            request.PolitenessLevel,
            request.Intro.TemplateParameters);
        var itemTexts = request.Items
            .Select((item, index) => ResolvePromptOrThrow(
                request.PromptContext,
                request.Item.PromptId,
                request.IsoCode,
                request.PolitenessLevel,
                request.Item.TemplateParametersFactory(index, item)))
            .ToImmutableArray();

        string? trailingText = null;
        if (request.Trailing is not null)
        {
            trailingText = ResolvePromptOrThrow(
                request.PromptContext,
                request.Trailing.PromptId,
                request.IsoCode,
                request.PolitenessLevel,
                request.Trailing.TemplateParameters);
        }

        return new SelectionDialogPrompt(introText, itemTexts, trailingText);
    }

    private static void AppendSegment(StringBuilder builder, string segment)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrWhiteSpace(segment))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append(segment.Trim());
    }
}
