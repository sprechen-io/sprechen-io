using Microsoft.Extensions.Logging;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Beobachtung bereits abgeschlossener Trace-ValueTasks: Fehler werden auf Debug protokolliert, Abbrüche ohne Logeintrag behandelt.
/// Die vorab abgeschlossenen Testwerte erlauben unmittelbare Assertions ohne zeitgesteuertes Warten.
/// </summary>
public sealed class SpeechAssistantTraceFaultObserverTests
{
    [Fact]
    public void Observe_synchronously_completed_trace_does_not_log_or_schedule()
    {
        var logger = new RecordingLogger<ISpeechAssistantService>();

        SpeechAssistantTraceFaultObserver.Observe(ValueTask.CompletedTask, logger);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Observe_faulted_trace_logs_the_fault_at_debug_without_throwing()
    {
        var logger = new RecordingLogger<ISpeechAssistantService>();
        var faulted = ValueTask.FromException(new InvalidOperationException("trace boom"));

        SpeechAssistantTraceFaultObserver.Observe(faulted, logger);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    [Fact]
    public void Observe_canceled_trace_is_swallowed_without_logging()
    {
        var logger = new RecordingLogger<ISpeechAssistantService>();
        var canceled = ValueTask.FromException(new OperationCanceledException());

        SpeechAssistantTraceFaultObserver.Observe(canceled, logger);

        Assert.Empty(logger.Entries);
    }
}
