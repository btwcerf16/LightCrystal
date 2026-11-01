using UnityEngine;

public abstract class Enchant : MonoBehaviour
{
    public EnchantData Data { get; private set; }
    public Weapon Weapon { get; private set; }
    public int Stacks { get; internal set; } = 1;
    public virtual int ExtraTargets => 0;
    protected float Value => Data.GetValue(Weapon.Rarity) * Stacks;

    public void Initialize(EnchantData data, Weapon weapon)
    {
        Data = data;
        Weapon = weapon;
    }

    public virtual void OnEquipped() { }
    public virtual void OnUnequipped() { }
    public virtual void OnHit(IDamageable target) { }
    public virtual void OnKill(ActorHealth target) { }
}
