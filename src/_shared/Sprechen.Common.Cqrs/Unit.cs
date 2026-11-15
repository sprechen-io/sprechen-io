namespace Sprechen.Common.Cqrs;

/// <summary>
/// Steht für kein Ergebnis, damit Befehle ohne Rückgabe denselben Handlervertrag
/// wie die übrigen nutzen.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value = new();

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}


