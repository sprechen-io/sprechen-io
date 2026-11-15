using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Gemeinsamer JSON-Konverter mit Unknown-Rückgabe für unbekannte Zeichenketten
/// oder andere gelesene Tokentypen. So können nachgelagerte Prüfungen ungültige
/// Modellwerte melden. Beim Schreiben sind nur gültige Übertragungswerte erlaubt.
/// Anders als <see cref="InputModeJsonConverter"/> lehnt die Read-Methode
/// unbekannte Werte nicht selbst mit einer JsonException ab.
/// </summary>
internal abstract class TolerantWireEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Ersatzwert für unbekannte Zeichenketten und andere Tokentypen.</summary>
    protected abstract TEnum Unknown { get; }

    /// <summary>
    /// Ordnet einen Enumwert seinem Übertragungswert zu. False kennzeichnet
    /// einen nicht serialisierbaren Wert. Abgeleitete Klassen können weitere Werte ablehnen.
    /// </summary>
    protected abstract bool TryToWire(TEnum value, out string wire);

    /// <summary>
    /// Ordnet eine bekannte Zeichenkette einem Enumwert zu. Bei false verwendet
    /// der Aufrufer den festgelegten Unknown-Wert.
    /// </summary>
    protected abstract bool TryFromWire(string? wire, out TEnum value);

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
        {
            return Unknown;
        }

        return TryFromWire(reader.GetString(), out var value) ? value : Unknown;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (!TryToWire(value, out var wire))
        {
            throw new JsonException(
                $"Cannot serialize sentinel {typeof(TEnum).Name}.{value} to a wire token.");
        }

        writer.WriteStringValue(wire);
    }
}
