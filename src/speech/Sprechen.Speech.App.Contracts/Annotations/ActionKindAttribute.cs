namespace Sprechen.Speech.App.Contracts.Annotations;

/// <summary>
/// Kennzeichnet Enums, deren Werte auszuführende Handlungen benennen.
/// Erlaubt für diese Handlungswerte verbartige statt zustandsbeschreibender Namen.
/// </summary>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class ActionKindAttribute : Attribute
{
}
