namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class AdmittedCaptureAttemptTests
{
    [Fact]
    public void Constructor_WithoutAdmission_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new AdmittedCaptureAttempt(admission: null!, speechAmplitudeThreshold: 350));

        Assert.Equal("admission", exception.ParamName);
    }

    [Theory]
    [InlineData(350)]
    [InlineData(7)]
    public void SpeechAmplitudeThreshold_IsCarriedUnchanged(int resolvedThreshold)
    {
        using var admission = new CaptureAttemptAdmission();

        var admittedAttempt = new AdmittedCaptureAttempt(admission, resolvedThreshold);

        // Der Prüfwert 7 liegt unter dem Mindestwert für Nutzereinstellungen.
        // Er muss hier unverändert bleiben, weil die Schwelle bereits aufgelöst wurde.
        Assert.Equal(resolvedThreshold, admittedAttempt.SpeechAmplitudeThreshold);
    }

    [Fact]
    public void Admission_IsTheSameInstance_BecauseItsLifetimeStaysWithTheSlotStore()
    {
        using var admission = new CaptureAttemptAdmission();

        var admittedAttempt = new AdmittedCaptureAttempt(admission, speechAmplitudeThreshold: 350);

        Assert.Same(admission, admittedAttempt.Admission);
    }
}
