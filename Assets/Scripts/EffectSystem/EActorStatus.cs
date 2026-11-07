using System;

[Flags]
public enum EActorStatus
{
    None = 0,
    Stunned = 1,
    Silenced = 2,
    Invulnerable = 4,
    Downed = 8
}
