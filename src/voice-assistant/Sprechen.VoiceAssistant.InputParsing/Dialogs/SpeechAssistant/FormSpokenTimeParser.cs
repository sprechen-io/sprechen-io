using System.Globalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert deutsche Uhrzeitäußerungen mit „Uhr“, „halb“, „viertel vor“ oder
/// „viertel nach“ anhand modellierter Schlüsselwörter. Stunden und Minuten können
/// Ziffern oder deutsche Zahlwörter sein. Umgangssprachliche Deutungen wie
/// „halb drei“ als 02:30 werden im Ergebnis benannt. AM/PM und „viertel drei“
/// werden hier nicht interpretiert. Unzulässige Stunden oder Minuten melden
/// <c>TimeOutOfRange</c>. Nicht erkannte Formen liefern <c>null</c>.
/// </summary>
internal sealed class FormSpokenTimeParser
{
    internal const string HalbAssumption = "assumed:halb-as-30-before-named-hour";
    internal const string ViertelVorAssumption = "assumed:viertel-vor-as-15-before-named-hour";
    internal const string ViertelNachAssumption = "assumed:viertel-nach-as-15-after-named-hour";

    private readonly FormSpokenNumberWordParser _numberWordParser;
    private readonly FormTimeInputKeywords _keywords;

    public FormSpokenTimeParser(FormSpokenNumberWordParser numberWordParser, FormTimeInputKeywords keywords)
    {
        _numberWordParser = numberWordParser ?? throw new ArgumentNullException(nameof(numberWordParser));
        _keywords = keywords ?? throw new ArgumentNullException(nameof(keywords));
    }

    public FormTranscriptParseOutcome? Parse(string transcript)
    {
        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var tokens = normalized.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return null;
        }

        if (tokens[0] == _keywords.HalfHour)
        {
            return ParseHalb(tokens);
        }

        if (tokens[0] == _keywords.Quarter)
        {
            return ParseViertel(tokens);
        }

        return ParseClock(tokens);
    }

    private FormTranscriptParseOutcome? ParseHalb(string[] tokens)
    {
        if (tokens.Length != 2 || !TryResolveNumber(tokens[1], out var namedHour))
        {
            return null;
        }

        if (namedHour < 1 || namedHour > 24)
        {
            return OutOfRange();
        }

        var hour = namedHour == 1 ? 0 : namedHour - 1;
        return hour > 23 ? OutOfRange() : Time(hour, 30, HalbAssumption);
    }

    private FormTranscriptParseOutcome? ParseViertel(string[] tokens)
    {
        // „viertel vor X“ und „viertel nach X“ verlangen drei Wörter.
        // „viertel X“ wird hier nicht interpretiert.
        if (tokens.Length != 3)
        {
            return null;
        }

        var nach = tokens[1] == _keywords.After;
        if (!nach && tokens[1] != _keywords.Before)
        {
            return null;
        }

        if (!TryResolveNumber(tokens[2], out var namedHour))
        {
            return null;
        }

        if (namedHour < 1 || namedHour > 24)
        {
            return OutOfRange();
        }

        if (nach)
        {
            return namedHour > 23 ? OutOfRange() : Time(namedHour, 15, ViertelNachAssumption);
        }

        var hour = namedHour == 1 ? 0 : namedHour - 1;
        return hour > 23 ? OutOfRange() : Time(hour, 45, ViertelVorAssumption);
    }

    private FormTranscriptParseOutcome? ParseClock(string[] tokens)
    {
        var uhrIndex = Array.IndexOf(tokens, _keywords.ClockMarker);
        if (uhrIndex < 0)
        {
            return null;
        }

        var before = tokens[..uhrIndex];
        var after = tokens[(uhrIndex + 1)..];

        string hourToken;
        string? minuteToken;
        if (before.Length == 1 && after.Length == 0)
        {
            hourToken = before[0];
            minuteToken = null;
        }
        else if (before.Length == 1 && after.Length == 1)
        {
            hourToken = before[0];
            minuteToken = after[0];
        }
        else if (before.Length == 2 && after.Length == 0)
        {
            // „14:30 Uhr“ und „14.30 Uhr“ werden zu „14 30 uhr“ normalisiert.
            hourToken = before[0];
            minuteToken = before[1];
        }
        else
        {
            return null;
        }

        if (!TryResolveNumber(hourToken, out var hour))
        {
            return null;
        }

        var minute = 0;
        if (minuteToken is not null && !TryResolveNumber(minuteToken, out minute))
        {
            return null;
        }

        return hour > 23 || minute > 59 ? OutOfRange() : Time(hour, minute, assumedInterpretation: null);
    }

    private bool TryResolveNumber(string token, out int value)
    {
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return _numberWordParser.TryParse(token, out value);
    }

    private static FormTranscriptParseOutcome Time(int hour, int minute, string? assumedInterpretation)
        => FormTranscriptParseOutcome.Succeeded(
            new TimeOnly(hour, minute).ToString("HH:mm", CultureInfo.InvariantCulture),
            assumedInterpretation);

    private static FormTranscriptParseOutcome OutOfRange()
        => FormTranscriptParseOutcome.Failed(new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.TimeOutOfRange));
}
