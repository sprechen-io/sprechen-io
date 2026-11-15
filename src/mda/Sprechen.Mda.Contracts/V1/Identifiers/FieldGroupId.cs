using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Identifiers;

/// <summary>
/// Typisierte Feldgruppenkennung mit JSON-Darstellung als Zeichenkette.
/// From verlangt eine nicht leere Angabe. Die Umwandlung aus string
/// ist ausdrücklich, die Umwandlung in string implizit.
/// </summary>
[JsonConverter(typeof(FieldGroupIdJsonConverter))]
public readonly record struct FieldGroupId
{
    private readonly string? _value;

    private FieldGroupId(string value) => _value = value;

    public string Value => _value ?? throw new InvalidOperationException("FieldGroupId is uninitialized.");

    public static FieldGroupId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new FieldGroupId(value);
    }

    public static implicit operator string(FieldGroupId id) => id.Value;

    public override string ToString() => Value;
}
