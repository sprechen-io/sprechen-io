namespace Sprechen.Speech.Runtime.Tests.Integration;

// ExpectedTokens enthält die normalisierten Solltokens für die Recall-Auswertung.
internal sealed record CorpusItem(string Spoken, string Kind, string[] ExpectedTokens);
