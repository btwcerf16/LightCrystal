using Unity.Netcode;
using UnityEngine;

public sealed class DownedEffect : Effect
{
    private float _reviveProgress;

    public override void EffectStart(ActorStats owner)
    {
        owner.MoveSpeed.AddModifier(new StatModifier(0.2f, StatModifierType.Multiplier, this));
        owner.PhysicalDamageMultiplier.AddModifier(new StatModifier(0.1f, StatModifierType.Multiplier, this));
        owner.MagicDamageMultiplier.AddModifier(new StatModifier(0.1f, StatModifierType.Multiplier, this));
    }

    public override void EffectTick(ActorStats owner, float deltaTime)
    {
        DownedEffectData data = (DownedEffectData)EffectData;
        float speed = 0f;
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(owner.transform.position, data.ReviveRadius))
        {
            PlayerInputReader player = collider.GetComponentInParent<PlayerInputReader>();
            if (player == null || player.gameObject == owner.gameObject
                || ((Vector2)(player.transform.position - owner.transform.position)).sqrMagnitude > data.ReviveRadius * data.ReviveRadius)
                continue;
            ActorHealth health = player.GetComponent<ActorHealth>();
            NetworkObject networkObject = player.GetComponent<NetworkObject>();
            if (health.IsDead || !networkObject.IsSpawned)
                continue;
            speed = Mathf.Max(speed, player.GetComponent<ActorStats>().ReviveSpeed.Value);
        }
        _reviveProgress = speed > 0f ? _reviveProgress + deltaTime * speed : Mathf.Max(0f, _reviveProgress - deltaTime);
        ActorHealth ownerHealth = owner.GetComponent<ActorHealth>();
        ownerHealth.ReviveProgress = Mathf.Clamp01(_reviveProgress / data.ReviveTime);
        if (_reviveProgress >= data.ReviveTime)
            ownerHealth.Revive(owner.MaxHealth.Value * data.ReviveHealthFraction);
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.GetComponent<ActorHealth>().ReviveProgress = 0f;
        owner.MoveSpeed.RemoveModifiersFrom(this);
        owner.PhysicalDamageMultiplier.RemoveModifiersFrom(this);
        owner.MagicDamageMultiplier.RemoveModifiersFrom(this);
    }
}
