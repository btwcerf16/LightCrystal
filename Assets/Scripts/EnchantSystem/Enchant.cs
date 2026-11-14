using UnityEngine;

public abstract class Enchant : MonoBehaviour
{
    public EnchantData Data { get; private set; }
    public Weapon Weapon { get; private set; }
    public int Stacks { get; internal set; } = 1;
    internal double CooldownEndsAt { get; set; }

    protected bool TryStartCooldown()
    {
        double now = Time.timeAsDouble;
        if (now < CooldownEndsAt)
            return false;

        CooldownEndsAt = now + Data.GetCooldown(Weapon.Rarity);
        return true;
    }

    protected float Value => Data.GetValue(Weapon.Rarity) * Stacks;

    public virtual void Initialize(EnchantData data, Weapon weapon)
    {
        Data = data;
        Weapon = weapon;
        Weapon.Equipped += OnEquipped;
        Weapon.Unequipped += OnUnequipped;
        Weapon.Hit += OnHit;
        Weapon.Killed += OnKill;
    }

    public virtual void OnEquipped() { }
    public virtual void OnUnequipped() { }
    internal virtual void Detach()
    {
        if (Weapon == null)
            return;
        Weapon.Equipped -= OnEquipped;
        Weapon.Unequipped -= OnUnequipped;
        Weapon.Hit -= OnHit;
        Weapon.Killed -= OnKill;
        OnUnequipped();
    }

    public virtual void OnHit(IDamageable target, EAttackType attackType, UnityEngine.Vector2 impactPoint) { }
    public virtual void OnKill(ActorHealth target) { }
}
