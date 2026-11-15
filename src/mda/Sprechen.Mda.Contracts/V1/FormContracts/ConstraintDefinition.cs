using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// <para>DE: Constraint-Definition (Validierungsregel), die ein Feld, eine Variable oder das gesamte Formular adressieren kann.</para>
/// <para>EN: Constraint definition (validation rule) that can target a field, variable, or the whole form.</para>
/// </summary>
/// <remarks>
/// <para>
/// DE: Constraints enthalten einen stabilen <c>code</c> (für Error-Codes) und eine lokalisierte <c>message</c>.
/// Für die rein auditive Bedienung soll <c>remediation</c> konkrete Hinweise zur Behebung enthalten.
/// </para>
/// <para>
/// EN: Constraints include a stable <c>code</c> (for error codes) and a localized <c>message</c>. For audio-only interaction, <c>remediation</c> should contain concrete "how to fix" guidance.
/// </para>
/// </remarks>
public sealed class ConstraintDefinition
{
    /// <summary>
    /// <para>DE: Stabile Kennung der Validierungsregel innerhalb des Vertrags.</para>
    /// <para>EN: Stable constraint identifier within the contract.</para>
    /// </summary>
    [JsonPropertyName("constraintId")]
    public string ConstraintId { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Zielreferenz für diesen Constraint.</para>
    /// <para>EN: Target reference for this constraint.</para>
    /// </summary>
    [JsonPropertyName("target")]
    public ConstraintTargetRef Target { get; set; } = new();

    /// <summary>
    /// <para>DE: Schweregrad des Constraint-Ergebnisses (z. B. error, warning, info).</para>
    /// <para>EN: Severity of the constraint result (e.g. error, warning, info).</para>
    /// </summary>
    [JsonPropertyName("severity")]
    public ConstraintSeverity Severity { get; set; }

    /// <summary>
    /// <para>DE: Stabiler anwendungsweiter Fehlercode. Die Zuordnung darf nicht vom Wortlaut einer Ausnahmemeldung abhängen.</para>
    /// <para>EN: Stable, application-level error code (do not rely on exception messages).</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Lokalisierte, menschenlesbare Fehlermeldung (was ist falsch?).</para>
    /// <para>EN: Localized human-facing message describing what is wrong.</para>
    /// </summary>
    [JsonPropertyName("message")]
    public LocalizedText Message { get; set; } = new();

    /// <summary>
    /// <para>DE: Optionaler lokalisierter Hinweis, der konkrete Schritte zur Behebung für die Sprachausgabe beschreibt.</para>
    /// <para>EN: Optional localized remediation text describing how to fix the issue (voice-friendly, concrete, actionable).</para>
    /// </summary>
    [JsonPropertyName("remediation")]
    public LocalizedText? Remediation { get; set; }

    /// <summary>
    /// <para>DE: Optionale Bedingung (Expression-Union), die den Constraint nur aktiviert, wenn sie <c>true</c> ergibt.</para>
    /// <para>EN: Optional condition (expression union) that activates this constraint only when it evaluates to true.</para>
    /// </summary>
    [JsonPropertyName("when")]
    public JsonElement? When { get; set; }

    /// <summary>
    /// <para>DE: Constraint-Typ (z. B. required, pattern, minLength, maxLength, ...).</para>
    /// <para>EN: Constraint kind (e.g. required, pattern, minLength, maxLength, ...).</para>
    /// </summary>
    [JsonPropertyName("kind")]
    public ConstraintKind Kind { get; set; }

    /// <summary>
    /// <para>DE: Optionaler fachlicher Scope des Constraints (field, fieldGroup, crossGroup, form).</para>
    /// <para>EN: Optional semantic scope of the constraint (field, fieldGroup, crossGroup, form).</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public ConstraintScope? Scope { get; set; }

    /// <summary>
    /// <para>DE: Typspezifische Zusatzdaten (z. B. <c>pattern</c>, <c>minLength</c>, <c>max</c>) als Erweiterungsdaten.</para>
    /// <para>EN: Kind-specific payload (e.g. <c>pattern</c>, <c>minLength</c>, <c>max</c>) captured as extension data.</para>
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
