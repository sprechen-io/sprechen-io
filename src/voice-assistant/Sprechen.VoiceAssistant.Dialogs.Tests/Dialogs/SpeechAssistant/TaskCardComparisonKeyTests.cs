using System.Text.Json;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class TaskCardComparisonKeyTests
{
    [Fact]
    public void FixtureRows_ShareOneKeyInThisRuntime()
    {
        var document = ReferenceTableResources.Read("task-card-comparison-key.reference.json");

        foreach (var row in document.RootElement.GetProperty("rows").EnumerateArray())
        {
            var expected = row.GetProperty("key").GetString();
            if (row.TryGetProperty("input", out var input))
            {
                Assert.Equal(expected, TaskCardComparisonKey.From(input.GetString()!));
                continue;
            }

            Assert.Equal(expected, TaskCardComparisonKey.From(row.GetProperty("left").GetString()!));
            Assert.Equal(expected, TaskCardComparisonKey.From(row.GetProperty("right").GetString()!));
        }
    }

}
