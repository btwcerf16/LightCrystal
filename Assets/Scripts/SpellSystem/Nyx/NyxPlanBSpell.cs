using System;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class NyxPlanBSpell : Spell
{
    private ActorHealth _health;
    private Crystal _crystal;
    private Tilemap _ground;
    private double _autoReadyAt;

    private NyxPlanBConfig Config => (NyxPlanBConfig)SpellData;

    public override void Activate()
    {
        base.Activate();
        _health = _owner.GetComponent<ActorHealth>();
        _health.TryRegisterLethalHitRecovery(PrepareEscape);
    }

    public override void Deactivate()
    {
        if (_health != null)
            _health.UnregisterLethalHitRecovery(PrepareEscape);
        base.Deactivate();
    }

    protected override void Cast(SpellCastData data) => Escape();

    private Action PrepareEscape()
    {
        double now = Time.timeAsDouble;
        if (!isActiveAndEnabled || !IsActive || now < _autoReadyAt || !_ownerSanity.TrySpendSanity(SanityCost))
            return null;

        _autoReadyAt = now + Config.AutoCooldown;
        return Escape;
    }

    private void Escape()
    {
        Vector3 origin = _owner.transform.position;
        NyxGraceWave wave = Instantiate(Config.WavePrefab, origin, Quaternion.identity);
        wave.NetworkObject.Spawn(destroyWithScene: true);

        HashSet<Enemy> stunnedEnemies = new();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, Config.WavePrefab.Radius))
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
            if (enemy != null && !enemy.GetComponent<ActorHealth>().IsDead && stunnedEnemies.Add(enemy))
                enemy.GetComponent<EffectHandler>().AddEffect(Config.Stun);
        }

        if (_crystal == null)
            _crystal = FindFirstObjectByType<Crystal>();
        if (_crystal == null)
            return;

        Vector2 toCrystal = _crystal.transform.position - origin;
        if (toCrystal.sqrMagnitude < 0.000001f)
            return;

        if (_ground == null)
            _ground = FindFirstObjectByType<ShopSpawnArea>()?.Ground;
        if (_ground == null)
            return;

        float distance = Mathf.Min(Config.BlinkDistance, toCrystal.magnitude);
        Vector3 destination = StudentBlinkSpell.FindDestination(_ground, origin, toCrystal, distance);
        if (destination == origin)
            return;

        _owner.GetComponent<PlayableActorMovement>().Stop();
        _owner.GetComponent<Rigidbody2D>().position = destination;
        _owner.GetComponent<NetworkTransform>().Teleport(destination, _owner.transform.rotation, _owner.transform.localScale);
    }
}
