using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Identifiers;

/// <summary>
/// Typisierte Variablenkennung mit JSON-Darstellung als Zeichenkette.
/// From verlangt eine nicht leere Angabe. Die Umwandlung aus string
/// ist ausdrücklich, die Umwandlung in string implizit.
/// </summary>
[JsonConverter(typeof(VariableIdJsonConverter))]
public readonly record struct VariableId
{
    private readonly string? _value;

    private VariableId(string value) => _value = value;

    public string Value => _value ?? throw new InvalidOperationException("VariableId is uninitialized.");

    public static VariableId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new VariableId(value);
    }

    public static implicit operator string(VariableId id) => id.Value;

    public override string ToString() => Value;
}
