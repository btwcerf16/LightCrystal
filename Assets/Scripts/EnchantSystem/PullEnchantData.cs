using UnityEngine;

[CreateAssetMenu(menuName = "Enchants/Pull")]
public sealed class PullEnchantData : EnchantData
{
    [Min(0.1f)] public float Radius = 3f;
    [Min(0.01f)] public float PullDuration = 0.25f;
    public LayerMask EnemyMask = 1 << 8;

    public override Enchant Create(Weapon weapon)
    {
        Enchant enchant = weapon.gameObject.AddComponent<PullEnchant>();
        enchant.Initialize(this, weapon);
        return enchant;
    }
}
