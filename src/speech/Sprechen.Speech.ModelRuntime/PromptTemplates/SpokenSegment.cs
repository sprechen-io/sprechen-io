namespace Sprechen.Speech.ModelRuntime.PromptTemplates;

/// <summary>
/// Hält Ansagetext und Ausgabesprache zusammen, damit mehrsprachige Abläufe jedes Segment mit der passenden Stimme ausgeben können.
/// </summary>
/// <remarks>
/// Der Sprachcode bleibt am Text, damit ein mehrsprachiger Ablauf jeden Abschnitt
/// in der vorgesehenen Sprache ausgeben kann. Der Wert selbst spielt keinen Ton ab.
/// </remarks>
/// <param name="Text">Der für die Sprachausgabe vorgesehene Text.</param>
/// <param name="IsoCode">Der Sprachcode für die zu verwendende Sprachausgabe.</param>
public sealed record SpokenSegment(string Text, string IsoCode);
