using System.Reflection;
using System.Security.Cryptography;
using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

/// <summary>
/// Vergleicht das eingebettete Transkriptlexikon des Produktionsprojekts byteweise mit der Testreferenz.
/// </summary>
public sealed class FormTranscriptLexiconEmbeddingIdentityTests
{
    private const string ProductionResourceName =
        "Sprechen.Service.Deepgram.Resources.form-transcript-lexicon.default.json";

    private const string TestResourceName =
        "Sprechen.Service.Deepgram.Tests.Resources.form-transcript-lexicon.default.json";

    [Fact]
    public void ProductionEmbedding_MatchesTestGoldenCopy()
    {
        var production = ReadResource(typeof(SpellingKeytermVocabulary).Assembly, ProductionResourceName);
        var golden = ReadResource(typeof(FormTranscriptLexiconEmbeddingIdentityTests).Assembly, TestResourceName);

        Assert.NotEmpty(production);
        Assert.Equal(production, golden);
        Assert.Equal(SHA256.HashData(production), SHA256.HashData(golden));
    }

    private static byte[] ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(resourceName);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
