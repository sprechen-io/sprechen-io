using Sprechen.Service.Audit.Services;
using Xunit;

namespace Sprechen.Service.Audit.Tests.Services;

public sealed class DeepgramApiVersionHeaderReaderTests
{
    [Fact]
    public void Read_ReturnsValueWhenHeaderPresent()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-DG-Model-Version"] = "2026-04-29.0"
        };

        var actual = DeepgramApiVersionHeaderReader.Read(headers);

        Assert.Equal("2026-04-29.0", actual);
    }

    [Fact]
    public void Read_IsCaseInsensitive()
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["x-dg-model-version"] = "v-lower"
        };

        var actual = DeepgramApiVersionHeaderReader.Read(headers);

        Assert.Equal("v-lower", actual);
    }

    [Fact]
    public void Read_ReturnsNullWhenHeaderMissing()
    {
        var headers = new Dictionary<string, string>
        {
            ["Other-Header"] = "x"
        };

        Assert.Null(DeepgramApiVersionHeaderReader.Read(headers));
    }

    [Fact]
    public void Read_ReturnsNullWhenInputIsNull()
    {
        Assert.Null(DeepgramApiVersionHeaderReader.Read(null));
    }

    [Fact]
    public void Read_ReturnsNullWhenValueIsWhitespace()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-DG-Model-Version"] = "   "
        };

        Assert.Null(DeepgramApiVersionHeaderReader.Read(headers));
    }

    [Fact]
    public void Read_TrimsValue()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-DG-Model-Version"] = "  v-trim  "
        };

        Assert.Equal("v-trim", DeepgramApiVersionHeaderReader.Read(headers));
    }
}
