using UnityEngine;

public sealed class PiercingEnchant : Enchant
{
    public override int ExtraTargets => Mathf.Max(0, Mathf.FloorToInt(Value));
}
