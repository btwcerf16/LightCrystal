using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ConsoleCommandAttribute : Attribute
{
    public bool ServerOnly { get; set; } = true;
}
