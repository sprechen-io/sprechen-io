namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Puffert Vorlauf und laufende Audioblöcke, damit ein noch unbestätigter Sprachbeginn
/// bei einem neuen Versuch erhalten und vorhandene Sprache als Teilsegment ausgegeben werden kann.
/// </summary>
internal sealed class PcmSpeechCaptureBufferedAudioState
{
    private readonly Queue<byte[]> _preRoll;
    private readonly Queue<int> _preSpeechAmplitudes;
    private readonly List<byte[]> _chunks;

    private int _tailEndChunkIndex = -1;

    public PcmSpeechCaptureBufferedAudioState(int maxPreRollChunks, int initialChunkCapacity)
    {
        _preRoll = new Queue<byte[]>(maxPreRollChunks);
        _preSpeechAmplitudes = new Queue<int>(maxPreRollChunks);
        _chunks = new List<byte[]>(initialChunkCapacity);
    }

    public int ChunkCount => _chunks.Count;
    public bool HasAdaptiveSpeechFloorSamples => _preSpeechAmplitudes.Count > 0;
    public bool HasInFlightChunks => _chunks.Count > 0;

    /// <summary>
    /// Erstellt <see cref="CapturedSpeechAudio"/> aus allen bisher erfassten Audioblöcken,
    /// auch wenn die abschließende Stillepause noch nicht erreicht ist.
    /// <c>FlushPartial</c> verwendet dies für <c>CaptureCancellationKind.Drain</c>.
    /// </summary>
    public CapturedSpeechAudio BuildPartialCapturedUtterance(SpeechAudioMimeType mimeType)
    {
        if (_chunks.Count == 0)
        {
            return new CapturedSpeechAudio(mimeType, Array.Empty<byte[]>());
        }

        return new CapturedSpeechAudio(mimeType, _chunks.ToArray());
    }

    public void EnqueuePreRoll(byte[] chunk, int averageAmplitude, bool trackCalibration, int maxPreRollChunks)
    {
        _preRoll.Enqueue(chunk);
        while (_preRoll.Count > maxPreRollChunks)
        {
            _preRoll.Dequeue();
        }

        if (trackCalibration)
        {
            _preSpeechAmplitudes.Enqueue(averageAmplitude);
        }

        while (_preSpeechAmplitudes.Count > maxPreRollChunks)
        {
            _preSpeechAmplitudes.Dequeue();
        }
    }

    public void ClearPreRollAndCalibration()
    {
        _preRoll.Clear();
        _preSpeechAmplitudes.Clear();
    }

    public void DrainPreRollIntoChunks()
    {
        while (_preRoll.TryDequeue(out var buffered))
        {
            _chunks.Add(buffered);
        }
    }

    public void AddChunk(byte[] chunk)
    {
        _chunks.Add(chunk);
    }

    public void MarkTailEndAtCurrentChunk()
    {
        _tailEndChunkIndex = _chunks.Count - 1;
    }

    public void MarkTailEndAt(int chunkIndex)
    {
        _tailEndChunkIndex = chunkIndex;
    }

    /// <summary>
    /// Übernimmt die Audioblöcke eines unbestätigten Sprachbeginns in ursprünglicher Reihenfolge
    /// zurück in den Vorlauf. Übersteigt dieser <paramref name="maxPreRollChunks"/>, werden
    /// die ältesten Blöcke entfernt. So können erhaltene Anlaute beim nächsten Versuch
    /// erneut ausgewertet werden. Die vorhandenen Kalibrieramplituden bleiben unverändert.
    /// Die zurückgegebenen Blöcke werden nicht als neue Hintergrundmessungen eingetragen.
    /// </summary>
    public void ReturnChunksToPreRoll(int maxPreRollChunks)
    {
        foreach (var chunk in _chunks)
        {
            _preRoll.Enqueue(chunk);
        }

        while (_preRoll.Count > maxPreRollChunks)
        {
            _preRoll.Dequeue();
        }

        _chunks.Clear();
        _tailEndChunkIndex = -1;
    }

    public int ResolveAdaptiveSpeechFloor()
    {
        return _preSpeechAmplitudes.Count == 0
            ? 0
            : _preSpeechAmplitudes.Max();
    }

    public CapturedSpeechAudio BuildCapturedUtterance(SpeechAudioMimeType mimeType)
    {
        if (_chunks.Count == 0)
        {
            return new CapturedSpeechAudio(mimeType, Array.Empty<byte[]>());
        }

        var finalIndex = _tailEndChunkIndex >= 0 && _tailEndChunkIndex < _chunks.Count
            ? _tailEndChunkIndex
            : _chunks.Count - 1;

        if (finalIndex < 0)
        {
            return new CapturedSpeechAudio(mimeType, Array.Empty<byte[]>());
        }

        if (finalIndex < _chunks.Count - 1)
        {
            _chunks.RemoveRange(finalIndex + 1, _chunks.Count - finalIndex - 1);
        }

        return new CapturedSpeechAudio(mimeType, _chunks);
    }
}
