using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class StudentBlinkSpell : Spell
{
    private Tilemap _ground;

    public override bool CanCast(SpellCastData data)
    {
        if (_ground == null)
            _ground = FindFirstObjectByType<ShopSpawnArea>()?.Ground;
        return _ground != null && _ground.HasTile(_ground.WorldToCell(transform.position));
    }

    public static Vector3 FindDestination(Tilemap ground, Vector3 origin, Vector2 direction, float range)
    {
        Vector3 destination = origin;
        direction.Normalize();
        float step = Mathf.Max(0.01f, Mathf.Min(ground.cellSize.x, ground.cellSize.y) * 0.25f);
        int steps = Mathf.CeilToInt(range / step);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 point = origin + (Vector3)(direction * Mathf.Min(i * step, range));
            if (!ground.HasTile(ground.WorldToCell(point)))
                break;
            destination = point;
        }
        return destination;
    }

    protected override void Cast(SpellCastData data)
    {
        StudentBlinkConfig config = (StudentBlinkConfig)SpellData;
        Vector2 direction = GetComponentInChildren<ActorHand>().AimDirection;
        Vector3 destination = FindDestination(_ground, transform.position, direction, config.Range);
        GetComponent<Rigidbody2D>().position = destination;
        GetComponent<NetworkTransform>().Teleport(destination, transform.rotation, transform.localScale);
        GetComponent<EffectHandler>().AddEffect(config.Stun);
    }
}
