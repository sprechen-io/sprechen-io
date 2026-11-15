using Grpc.Core;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class TextToSpeechPrePcmEnqueueRetryPolicyTests
{
    private const string MeasuredDetail =
        "Error reading next message. HttpRequestException: TypeError: network error JSException: TypeError: network error";

    private readonly TextToSpeechPrePcmEnqueueRetryPolicy _policy = new();

    [Fact]
    public void Evaluate_MeasuredFirstReadNetworkError_IsEligible()
    {
        var result = _policy.Evaluate(Failure(1, StatusCode.Internal, MeasuredDetail, TextToSpeechPcmEnqueueState.NotStarted));
        Assert.Equal(TextToSpeechRetryEligibility.Eligible, result);
    }

    [Fact]
    public void Evaluate_SecondAttempt_IsIneligible()
    {
        var result = _policy.Evaluate(Failure(2, StatusCode.Internal, MeasuredDetail, TextToSpeechPcmEnqueueState.NotStarted));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_PossiblyEnqueued_IsIneligible()
    {
        var result = _policy.Evaluate(Failure(1, StatusCode.Internal, MeasuredDetail, TextToSpeechPcmEnqueueState.PossiblyEnqueued));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_Enqueued_IsIneligible()
    {
        var result = _policy.Evaluate(Failure(1, StatusCode.Internal, MeasuredDetail, TextToSpeechPcmEnqueueState.Enqueued));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_BareInternalDown_IsIneligible()
    {
        var result = _policy.Evaluate(Failure(1, StatusCode.Internal, "down", TextToSpeechPcmEnqueueState.NotStarted));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_FailedToFetchWithoutReadingNextMessage_IsIneligible()
    {
        var result = _policy.Evaluate(
            Failure(1, StatusCode.Internal, "HttpRequestException: TypeError: Failed to fetch", TextToSpeechPcmEnqueueState.NotStarted));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_CallerCancelled_IsIneligible()
    {
        var result = _policy.Evaluate(
            Failure(1, StatusCode.Internal, MeasuredDetail, TextToSpeechPcmEnqueueState.NotStarted, callerCancelled: true));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_PlaybackSupersessionCancelled_IsIneligible()
    {
        var result = _policy.Evaluate(
            Failure(
                1,
                StatusCode.Internal,
                MeasuredDetail,
                TextToSpeechPcmEnqueueState.NotStarted,
                playbackCancelled: true));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_BargeInCancelled_IsIneligible()
    {
        var result = _policy.Evaluate(
            Failure(1, StatusCode.Internal, MeasuredDetail, TextToSpeechPcmEnqueueState.NotStarted, bargeInCancelled: true));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_NetworkErrorWithoutReadingNextMessage_IsIneligible()
    {
        var result = _policy.Evaluate(
            Failure(
                1,
                StatusCode.Internal,
                "HttpRequestException: TypeError: network error",
                TextToSpeechPcmEnqueueState.NotStarted));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    [Fact]
    public void Evaluate_UnavailableStatus_IsIneligible()
    {
        var result = _policy.Evaluate(Failure(1, StatusCode.Unavailable, MeasuredDetail, TextToSpeechPcmEnqueueState.NotStarted));
        Assert.Equal(TextToSpeechRetryEligibility.Ineligible, result);
    }

    private static TextToSpeechAttemptFailure Failure(
        int attempt,
        StatusCode status,
        string detail,
        TextToSpeechPcmEnqueueState enqueue,
        bool callerCancelled = false,
        bool playbackCancelled = false,
        bool bargeInCancelled = false)
    {
        return new TextToSpeechAttemptFailure(
            attempt,
            status,
            detail,
            enqueue,
            callerCancelled,
            playbackCancelled,
            bargeInCancelled);
    }
}
