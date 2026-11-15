using System.Collections.Immutable;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Strukturierte Dialogentscheidung mit einem Namen und benannten Feldern.</summary>
/// <remarks>
/// Verbraucher lesen Namen und Felder unabhängig von der Darstellung als Textzeile.
/// So können Browserkonsole und serverseitige Aufzeichnung dieselbe Entscheidung
/// unterschiedlich darstellen, ohne dass der Dialog diese Darstellung festlegen muss.
/// </remarks>
public sealed record DialogDecision(string Name, ImmutableArray<DialogDecisionField> Fields);
