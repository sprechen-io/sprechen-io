using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.App.CaptureRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft explizite Implementierungen von <see cref="IModeAwareSpeechCaptureClient"/>
/// am Produktionsclient und ausgewählten Testdoubles. Ergänzende Tests prüfen erfasste
/// Erkennungsparameter, Stilleabbruch und kontrolliert asynchronen Abschluss.
/// </summary>
public sealed class ModeAwareSpeechCaptureContractTheories
{
    [Fact]
    public void ProductionSpeechCaptureClient_OverridesEveryContractMember()
    {
        var productionType = typeof(SpeechCaptureClient);
        var interfaceType = typeof(IModeAwareSpeechCaptureClient);
        var map = productionType.GetInterfaceMap(interfaceType);

        for (var index = 0; index < map.InterfaceMethods.Length; index++)
        {
            var interfaceMethod = map.InterfaceMethods[index];
            var implementation = map.TargetMethods[index];
            Assert.True(
                implementation.DeclaringType == productionType,
                $"Production client must implement {interfaceMethod.Name} explicitly, not inherit a default.");
        }
    }

    [Fact]
    public void ProductionSpeechCaptureClient_HonorsTheCursorContract()
    {
        var productionType = typeof(SpeechCaptureClient);

        var snapshot = productionType.GetMethod(
            nameof(IModeAwareSpeechCaptureClient.SnapshotLiveCaptureCursor));
        Assert.NotNull(snapshot);
        Assert.Equal(productionType, snapshot.DeclaringType);

        var cursorOverload = productionType
            .GetMethods()
            .SingleOrDefault(method =>
                method.Name == nameof(IModeAwareSpeechCaptureClient.RecordAsync)
                && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(SampleCursor)));
        Assert.NotNull(cursorOverload);
        Assert.Equal(productionType, cursorOverload.DeclaringType);
    }

    [Theory]
    [InlineData(typeof(SpeechCaptureClient))]
    [InlineData(typeof(ScriptedSpeechInputClient))]
    [InlineData(typeof(Sprechen.Speech.TestDoubles.QueuedSpeechInputClient))]
    [InlineData(typeof(DeepgramTranscribingSpeechInputClient))]
    public void EveryImplementer_TakesAnExplicitPosition_OnEveryContractMember(Type implementerType)
    {
        // Jeder hier aufgeführte Typ soll die Vertragsmethoden selbst bereitstellen.
        var interfaceType = typeof(IModeAwareSpeechCaptureClient);
        var map = implementerType.GetInterfaceMap(interfaceType);

        for (var index = 0; index < map.InterfaceMethods.Length; index++)
        {
            var interfaceMethod = map.InterfaceMethods[index];
            var implementation = map.TargetMethods[index];
            Assert.True(
                implementation.DeclaringType == implementerType
                    || (implementation.DeclaringType is not null
                        && implementation.DeclaringType != interfaceType
                        && implementerType.IsAssignableFrom(implementation.DeclaringType)),
                $"{implementerType.Name} must implement {interfaceMethod.Name} explicitly.");
        }
    }

    [Fact]
    public async Task ScriptedDouble_RecordsLanguageProfileAndKeyterms_InsteadOfIgnoringThem()
    {
        var client = new ScriptedSpeechInputClient();
        client.EnqueueTranscript("eins");
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        await client.SpeechToTextAsync(
            utterance,
            "de",
            RecognitionProfile.Conversational,
            CancellationToken.None,
            new SpeechKeyterms(["eins", "zwei"], KeytermComposition.WithServerBase));

        var call = Assert.Single(client.SpeechToTextCalls);
        Assert.Equal("de", call.LanguageCode);
        Assert.Equal(RecognitionProfile.Conversational, call.Profile);
        Assert.Equal(["eins", "zwei"], call.Keyterms!.Terms);
    }

    [Fact]
    public async Task ScriptedDouble_EnforcesTheInitialSilenceTimeout_AsNoSpeech()
    {
        var client = new ScriptedSpeechInputClient();

        // Die leere Queue muss NoSpeech liefern; das äußere Timeout begrenzt den Test bei einem Fehler.
        await Assert.ThrowsAsync<NoSpeechDetectedException>(
            () => client.RecordAsync(
                SpeechCaptureMode.Listening,
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ScriptedDouble_SpeechToTextCompletesAsynchronously()
    {
        var client = new ScriptedSpeechInputClient();
        client.EnqueueTranscript("eins");
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        // Die Freigabe steuert den Abschluss ausdrücklich, damit die Assertion keinen Threadwechsel erraten muss.
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.HoldCallsUntil(release.Task);

        var task = client.SpeechToTextAsync(utterance, "de", RecognitionProfile.Conversational, CancellationToken.None);

        Assert.False(task.IsCompleted);
        release.SetResult();
        Assert.Equal("eins", (await task).Text);
    }

    [Theory]
    [MemberData(nameof(QueueDoubles))]
    public void QueueDoubles_DeclareAnExplicitNoRingPosition(Func<IModeAwareSpeechCaptureClient> factory)
    {
        // Diese Doubles haben keinen Aufnahmering und geben deshalb bewusst keinen Cursor zurück.
        var instance = factory();
        var snapshot = instance.GetType().GetMethod(nameof(IModeAwareSpeechCaptureClient.SnapshotLiveCaptureCursor));
        Assert.NotNull(snapshot);
        Assert.Equal(instance.GetType(), snapshot.DeclaringType);
        Assert.Null(snapshot.Invoke(instance, null));
    }

    public static IEnumerable<object[]> QueueDoubles =>
    [
        [() => new Sprechen.Speech.TestDoubles.QueuedSpeechInputClient()],
        [() => new DeepgramTranscribingSpeechInputClient(
            (_, _, _) => Task.FromResult(new SpeechTranscript("probe", null)))],
    ];
}
