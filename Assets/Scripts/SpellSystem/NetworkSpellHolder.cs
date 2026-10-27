using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpellHolder))]
public sealed class NetworkSpellHolder : NetworkBehaviour
{
    private NetworkList<double> _cooldowns;
    private SpellHolder _holder;

    private void Awake()
    {
        _holder = GetComponent<SpellHolder>();
        _cooldowns = new NetworkList<double>();
    }

    public override void OnNetworkSpawn()
    {
        _holder.Initialize(IsServer);
        if (IsServer)
        {
            _cooldowns.Clear();
            for (int i = 0; i < _holder.Spells.Count; i++)
                _cooldowns.Add(0);
            _holder.SpellCast += PublishCooldown;
        }
        else
        {
            _cooldowns.OnListChanged += ReceiveCooldowns;
            ReceiveCooldowns(default);
        }
        if (IsOwner)
            _holder.CastRequested += RequestCast;
    }

    public override void OnNetworkDespawn()
    {
        _holder.CastRequested -= RequestCast;
        _holder.SpellCast -= PublishCooldown;
        _cooldowns.OnListChanged -= ReceiveCooldowns;
        _holder.ClearSpells();
    }

    private void RequestCast(int index, SpellCastData data)
    {
        NetworkObjectReference target = new NetworkObjectReference((NetworkObject)null);
        if (data.Target != null)
        {
            NetworkObject targetObject = data.Target.GetComponentInParent<NetworkObject>();
            if (targetObject != null && targetObject.IsSpawned)
                target = targetObject;
        }
        CastSpellRpc(index, data.Position, data.Direction, target);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void CastSpellRpc(int index, Vector2 position, Vector2 direction, NetworkObjectReference target)
    {
        if (!float.IsFinite(position.x) || !float.IsFinite(position.y) ||
            !float.IsFinite(direction.x) || !float.IsFinite(direction.y))
            return;
        target.TryGet(out NetworkObject targetObject, NetworkManager);
        _holder.CastSpell(index, new SpellCastData
        {
            Position = position,
            Direction = direction,
            Target = targetObject != null ? targetObject.gameObject : null,
            Caster = gameObject
        });
    }

    private void PublishCooldown(int index)
    {
        _cooldowns[index] = _holder.Spells[index].CooldownEndsAt;
    }

    private void ReceiveCooldowns(NetworkListEvent<double> change)
    {
        for (int i = 0; i < _cooldowns.Count && i < _holder.Spells.Count; i++)
        {
            Spell spell = _holder.Spells[i];
            if (spell != null)
                spell.SetCooldown(_cooldowns[i]);
        }
    }
}
