namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Hält eine freie Sprungzielphrase für die anschließende Zielauflösung.
/// Die Phrase ist keine typisierte Vertragskennung. Leere Werte sind erlaubt.
/// Normalisierung und Zuordnung erfolgen im Resolver.
/// </summary>
internal readonly record struct FormJumpTarget
{
    private readonly string? _phrase;

    public FormJumpTarget(string phrase) => _phrase = phrase;

    public string Value => _phrase ?? string.Empty;

    public static FormJumpTarget From(string phrase) => new(phrase);

    public static implicit operator string(FormJumpTarget target) => target.Value;

    public override string ToString() => Value;
}
