using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Identifiers;

/// <summary>
/// Typisierte Befehlskennung mit JSON-Darstellung als Zeichenkette.
/// From verlangt eine nicht leere Angabe. Die Umwandlung aus string
/// ist ausdrücklich, die Umwandlung in string implizit.
/// </summary>
[JsonConverter(typeof(CommandIdJsonConverter))]
public readonly record struct CommandId
{
    private readonly string? _value;

    private CommandId(string value) => _value = value;

    public string Value => _value ?? throw new InvalidOperationException("CommandId is uninitialized.");

    public static CommandId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new CommandId(value);
    }

    public static implicit operator string(CommandId id) => id.Value;

    public override string ToString() => Value;
}
