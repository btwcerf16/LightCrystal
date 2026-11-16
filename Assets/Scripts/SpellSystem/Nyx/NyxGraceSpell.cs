using System.Collections.Generic;
using UnityEngine;

public sealed class NyxGraceSpell : Spell
{
    protected override void Cast(SpellCastData data)
    {
        NyxGraceConfig config = (NyxGraceConfig)SpellData;
        Vector2 direction = data.Direction.sqrMagnitude > 0f ? data.Direction.normalized : _owner.GetComponentInChildren<ActorHand>().AimDirection;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 center = _owner.transform.position + (Vector3)(direction * config.WavePrefab.Radius);
        NyxGraceWave wave = Instantiate(config.WavePrefab, center, Quaternion.Euler(0f, 0f, angle));
        wave.NetworkObject.Spawn(destroyWithScene: true);
        HitWave(center, config);
        _owner.GetComponent<PlayableActorMovement>().ApplyKnockback(-direction, config.HopSpeed, config.HopDuration);
    }

    private void HitWave(Vector2 center, NyxGraceConfig config)
    {
        HashSet<ActorHealth> hitEnemies = new();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, config.WavePrefab.Radius))
        {
            if (hit == null)
                continue;
            if (hit.CompareTag("Projectile") && hit.TryGetComponent(out Projectile projectile))
            {
                if (projectile.IsFlying)
                    projectile.Finish();
                continue;
            }

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.TryGetComponent(out ActorHealth health) || health.IsDead || !hitEnemies.Add(health))
                continue;
            health.TakeDamage(config.Damage, EDamageType.Physical, _ownerStats, critical: false, isMelee: true);
            if (!health.IsDead)
                enemy.GetComponent<EffectHandler>().AddEffect(config.Stun);
        }
    }
}
