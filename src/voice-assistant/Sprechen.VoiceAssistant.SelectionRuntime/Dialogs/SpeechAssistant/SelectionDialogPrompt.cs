using System.Collections.Immutable;
using System.Text;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet bereinigte Einleitung, Listeneinträge und Abschluss einer Auswahlansage, damit
/// Wiedergabe und Diagnose dieselbe Textzusammensetzung verwenden.
/// </summary>
public sealed class SelectionDialogPrompt
{
    public SelectionDialogPrompt(
        string? introText,
        ImmutableArray<string> itemTexts,
        string? trailingText)
    {
        IntroText = Normalize(introText);
        ItemTexts = itemTexts
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .ToImmutableArray();
        TrailingText = Normalize(trailingText);
        Text = BuildText(IntroText, ItemTexts, TrailingText);
    }

    /// <summary>
    /// Trägt die Rückmeldeart von ihrer Entscheidungsstelle bis zur Wiedergabe mit. Die Ausgabestelle
    /// muss dadurch nicht selbst erschließen, ob ein Text etwa fehlende Eingabe oder einen Fehler
    /// meldet.
    /// </summary>
    public SpokenAnnouncementKind AnnouncementKind { get; private init; } = SpokenAnnouncementKind.Prompt;

    /// <summary>
    /// Liefert eine Ansage mit gleicher Textzusammensetzung und gewünschter Rückmeldeart. Nur bei
    /// bereits gleicher Art wird dieselbe Instanz zurückgegeben.
    /// </summary>
    public SelectionDialogPrompt AsAnnouncement(SpokenAnnouncementKind announcementKind)
        => announcementKind == AnnouncementKind
            ? this
            : new SelectionDialogPrompt(IntroText, ItemTexts, TrailingText) { AnnouncementKind = announcementKind };

    public string? IntroText { get; }

    public ImmutableArray<string> ItemTexts { get; }

    public string? TrailingText { get; }

    public string Text { get; }

    public bool HasContent => Text.Length > 0;

    public static SelectionDialogPrompt FromText(string? text)
    {
        return new SelectionDialogPrompt(text, ImmutableArray<string>.Empty, trailingText: null);
    }

    public static SelectionDialogPrompt FromSequence(
        string? introText,
        IEnumerable<string> itemTexts,
        string? trailingText = null)
    {
        ArgumentNullException.ThrowIfNull(itemTexts);
        return new SelectionDialogPrompt(introText, itemTexts.ToImmutableArray(), trailingText);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildText(
        string? introText,
        ImmutableArray<string> itemTexts,
        string? trailingText)
    {
        var builder = new StringBuilder();
        AppendSegment(builder, introText);

        foreach (var itemText in itemTexts)
        {
            AppendSegment(builder, itemText);
        }

        AppendSegment(builder, trailingText);
        return builder.ToString().Trim();
    }

    private static void AppendSegment(StringBuilder builder, string? segment)
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
