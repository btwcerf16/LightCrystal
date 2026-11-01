public sealed class BurnEnchant : Enchant
{
    public override void OnHit(IDamageable target)
    {
        ActorHealth health = target as ActorHealth;
        if (health == null || health.IsDead)
            return;
        EffectHandler effects = health.GetComponent<EffectHandler>();
        BurnEffectData data = ((BurnEnchantData)Data).Effect;
        if (effects != null && data != null)
        {
            BurnEffect effect = effects.AddEffect(data) as BurnEffect;
            if (effect != null)
                effect.Configure(Weapon, Value);
        }
    }
}
