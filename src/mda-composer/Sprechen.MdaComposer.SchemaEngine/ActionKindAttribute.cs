namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Kennzeichnet Enums, deren Werte auszuführende Handlungen benennen.
/// Erlaubt für diese Handlungswerte verbartige statt zustandsbeschreibender Namen.
/// Die lokale Definition vermeidet eine Abhängigkeit vom Speech-Vertrag.
/// </summary>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
internal sealed class ActionKindAttribute : Attribute
{
}
