namespace Sprechen.Service.Abstractions;

/// <summary>
/// Normalisierter Sprachcode aus zwei Buchstaben. Ein angehängter Regionsteil wird entfernt.
/// </summary>
public readonly record struct LanguageCode
{
    private readonly string? _value;

    public string Value => _value ?? throw new InvalidOperationException("LanguageCode is uninitialized.");

    private LanguageCode(string value)
    {
        _value = value;
    }

    public static LanguageCode ParseIso6391(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);

        if (!TryParseIso6391(raw, out var code))
        {
            throw new ArgumentException("Expected ISO 639-1 (two letters).", nameof(raw));
        }

        return code;
    }

    public static bool TryParseIso6391(string? raw, out LanguageCode code)
    {
        code = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var token = raw.Trim();
        var dashIndex = token.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            token = token[..dashIndex];
        }

        token = token.ToLowerInvariant();

        if (token.Length != 2 || !token.All(char.IsLetter))
        {
            return false;
        }

        code = new LanguageCode(token);
        return true;
    }

    public override string ToString() => Value;
}

